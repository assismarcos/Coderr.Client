﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using Coderr.Client.Contracts;

namespace Coderr.Client.Converters
{
    /// <summary>
    ///     Converts an object into a context collection.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The following conversions are supported:
    ///     </para>
    ///     <list type="table">
    ///         <listheader>
    ///             <term>type</term>
    ///             <description>description</description>
    ///         </listheader>
    ///         <item>
    ///             <term>anonymous object</term>
    ///             <description>Collection will be named <c>CustomData</c>. All properties will be included</description>
    ///         </item>
    ///         <item>
    ///             <term>class</term>
    ///             <description>Collection will be named as the class. All properties will be included</description>
    ///         </item>
    ///         <item>
    ///             <term>
    ///                 <see cref="ContextCollectionDTO" />
    ///             </term>
    ///             <description>Collection is included directly</description>
    ///         </item>
    ///         <item>
    ///             <term>
    ///                 <c>ContextCollectionDTO[]</c>
    ///             </term>
    ///             <description>All collections will be added as different ones (and not nested)</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    public class ObjectToContextCollectionConverter
    {
        private readonly MethodInfo _dictionaryConverterMethod;
        private readonly MethodInfo _keyValuePairEnumeratorConverterMethod;
        private string[] _propertiesToIgnore = new string[0];
        private Func<string, object, bool> _propertyFilter;

        /// <summary>
        ///     Creates a new instance of <see cref="ObjectToContextCollectionConverter" />.
        /// </summary>
        public ObjectToContextCollectionConverter()
        {
            MaxPropertyCount = 10000;
            _dictionaryConverterMethod = GetType()
                .GetMethod("ConvertDictionary", BindingFlags.Instance | BindingFlags.NonPublic);
            _keyValuePairEnumeratorConverterMethod = GetType()
                .GetMethod("ConvertKvpEnumerator", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        /// <summary>
        ///     Maximum number of properties that can be added during the collection process.
        /// </summary>
        /// <value>
        ///     Default is 10,000
        /// </value>
        public int MaxPropertyCount { get; set; }

        /// <summary>
        ///     Turn an object into a string which can be used for debugging.
        /// </summary>
        /// <param name="collectionName">
        ///     Name of the collection that is being created. This name is displayed under "Similarities"
        ///     and "Context info" in our UI.
        /// </param>
        /// <param name="instance">Object to get a string representation for</param>
        /// <returns>"null" if the object is null, otherwise an string as given per object sample</returns>
        /// <remarks>
        ///     Look at the class doc for an example.
        /// </remarks>
        public ContextCollectionDTO Convert(string collectionName, object instance)
        {
            if (instance == null)
                throw new ArgumentNullException("instance");
            if (IsFilteredOut(instance))
                return new ContextCollectionDTO(collectionName,
                    new Dictionary<string, string> {{"Error", "The object type can not be traversed by codeRR"}});

            try
            {
                var dictIf = GetGenericDictionaryInterface(instance);
                if (dictIf != null)
                {
                    var contextCollection = new ContextCollectionDTO(collectionName);
                    var path = new List<object>();
                    _dictionaryConverterMethod.MakeGenericMethod(dictIf.GetGenericArguments())
                        .Invoke(this, new[] {"", instance, contextCollection, path});
                    return contextCollection;
                }
                var kvpTypes = GetKeyValuePairFromEnumeratorInterface(instance);
                if (kvpTypes != null)
                {
                    var contextCollection = new ContextCollectionDTO(collectionName);
                    var path = new List<object>();
                    _keyValuePairEnumeratorConverterMethod.MakeGenericMethod(kvpTypes)
                        .Invoke(this, new[] {"", instance, contextCollection, path});
                    return contextCollection;
                }
                if (instance is ContextCollectionDTO)
                    return (ContextCollectionDTO) instance;
                if (instance is IDictionary<string, string>)
                    return new ContextCollectionDTO(collectionName, (IDictionary<string, string>) instance);
                if (instance is NameValueCollection)
                    return new ContextCollectionDTO(collectionName, (NameValueCollection) instance);
                if (instance is IDictionary)
                    return ConvertDictionaryToCollection(collectionName, (IDictionary) instance);
                var collection = new ContextCollectionDTO(collectionName);
                if (IsSimpleType(instance.GetType()))
                {
                    collection.Properties.Add("Value", instance.ToString());
                }
                else
                {
                    var path = new List<object>();
                    ReflectObject(instance, "", collection, path);
                }

                return collection;
            }
            catch (Exception exception)
            {
                return new ContextCollectionDTO("CoderrClientError",
                    new Dictionary<string, string>
                    {
                        {"Exception", exception.ToString()},
                        {"Type", instance.GetType().FullName},
                        {"Source", "ObjectToContextCollectionConverter"}
                    });
            }
        }

        /// <summary>
        ///     Turn an object into a string which can be used for debugging.
        /// </summary>
        /// <param name="instance">Object to get a string representation for</param>
        /// <returns>"null" if the object is null, otherwise an string as given per object sample</returns>
        /// <remarks>
        ///     <para>
        ///         Collection name will be <c>ContextData</c> unless the object is a real type which is not a collection (in that
        ///         case the type name is used).
        ///     </para>
        ///     <para>Look at the class doc for an example.</para>
        /// </remarks>
        public ContextCollectionDTO Convert(object instance)
        {
            switch (instance)
            {
                case null:
                    throw new ArgumentNullException(nameof(instance));
                case ContextCollectionDTO dto:
                    return dto;
                case IDictionary<string, string> dictionary:
                    return new ContextCollectionDTO("ContextData", dictionary);
                case NameValueCollection collection:
                    return new ContextCollectionDTO("ContextData", collection);
            }

            var name = instance.GetType().IsAnonymousType()
                ? "ContextData"
                : instance.GetType().Name;


            return Convert(name, instance);
        }

        /// <summary>
        ///     Filter properties.
        /// </summary>
        /// <param name="propertyFilter">propertyName, propertyValue</param>
        /// <returns><c>true</c> if property should be filtered out; otherwise <c>false</c></returns>
        public void FilterProperties(Func<string, object, bool> propertyFilter)
        {
            _propertyFilter = propertyFilter ?? throw new ArgumentNullException(nameof(propertyFilter));
        }

        /// <summary>
        ///     Properties that should be ignored when the context collection is being built.
        /// </summary>
        /// <param name="properties">Case sensitive names</param>
        public void Ignore(params string[] properties)
        {
            _propertiesToIgnore = properties ?? throw new ArgumentNullException(nameof(properties));
        }

        /// <summary>
        ///     Checks if the specified type could be traversed or just added as a value.
        /// </summary>
        /// <param name="type">Type to check</param>
        /// <returns><c>true</c> if we should add this type as a value; <c>false</c> if we should do reflection on it.</returns>
        public bool IsSimpleType(Type type)
        {
            return type.IsPrimitive
                   || type == typeof(decimal)
                   || type == typeof(string)
                   || type == typeof(DateTime)
                   || type == typeof(Guid)
                   || type == typeof(DateTimeOffset)
                   || type == typeof(NumberFormatInfo)
                   || type == typeof(DateTimeFormatInfo)
                   || type == typeof(TimeSpan)
                   || type.IsEnum
                   || type.FullName == "System.Drawing.Color";
        }

        /// <summary>
        ///     Use reflection on a complex object to add it's values to our context collection
        /// </summary>
        /// <param name="instance">Current object to reflect</param>
        /// <param name="prefix">Prefix, like "User.Address.Postal.ZipCode"</param>
        /// <param name="contextCollection">Collection that values should be added to.</param>
        /// <param name="path">To prevent circular references.</param>
        protected void ReflectObject(object instance, string prefix, ContextCollectionDTO contextCollection,
            List<object> path)
        {
            if (path.Contains(instance) || path.Count > 10 || MaxPropertyCount <= contextCollection.Properties.Count)
            {
                contextCollection.Properties.Add(prefix + "_error", "Circular reference or to deep hierarchy.");
                return;
            }
            if (IsFilteredOut(instance))
                return;

            path.Add(instance);

            foreach (var propInfo in instance.GetType().GetProperties())
            {
                //TODO: Add support.
                if (propInfo.GetIndexParameters().Length != 0)
                    continue;

                if (_propertiesToIgnore.Contains(propInfo.Name))
                    continue;

                var propertyName = propInfo.Name;
                object value;
                try
                {
                    value = propInfo.GetValue(instance, null);
                }
                catch (Exception exception)
                {
                    contextCollection.Properties.Add(prefix + propertyName + "._error", exception.ToString());
                    continue;
                }

                if (_propertyFilter?.Invoke(propInfo.Name, value) == true)
                    continue;

                switch (value)
                {
                    case null:
                        contextCollection.Properties.Add(prefix + propertyName, "null");
                        continue;
                    case Encoding enc:
                        contextCollection.Properties.Add(prefix + propertyName, enc.EncodingName);
                        continue;
                    case DateTimeFormatInfo v1:
                        contextCollection.Properties.Add(prefix + propertyName, v1.NativeCalendarName);
                        continue;
                    case CultureInfo v2:
                        contextCollection.Properties.Add(prefix + propertyName, "Culture[" + v2.LCID + "]");
                        continue;
                }

                if (IsSimpleType(value.GetType()) || propertyName == "Encoding")
                {
                    contextCollection.Properties.Add(prefix + propertyName, value.ToString());
                }
                else
                {
                    var dictIf = GetGenericDictionaryInterface(value);
                    var kvpTypes = GetKeyValuePairFromEnumeratorInterface(value);
                    if (dictIf != null)
                    {
                        _dictionaryConverterMethod.MakeGenericMethod(dictIf.GetGenericArguments())
                            .Invoke(this, new[] {propertyName, value, contextCollection, path});
                    }
                    else if (kvpTypes != null)
                    {
                        _keyValuePairEnumeratorConverterMethod.MakeGenericMethod(kvpTypes)
                            .Invoke(this, new[] {propertyName, value, contextCollection, path});
                    }
                    else if (value is IDictionary)
                    {
                        var items = value as IDictionary;
                        foreach (DictionaryEntry kvp in items)
                        {
                            var dictPropName = kvp.Key?.ToString() ?? "null";
                            var newPrefix = $"{prefix}{propertyName}[{dictPropName}]";
                            ReflectValue(newPrefix, kvp.Value, contextCollection,
                                path);
                        }
                    }
                    else if (value is IEnumerable)
                    {
                        var items = value as IEnumerable;
                        var index = 0;
                        foreach (var item in items)
                        {
                            var newPrefix = prefix == ""
                                ? $"{propertyName}[{index}]"
                                : $"{prefix}{propertyName}[{index}]";
                            ReflectValue(newPrefix, item, contextCollection, path);
                            index++;
                        }
                    }
                    else
                    {
                        var newPrefix = prefix == ""
                            ? propertyName + "."
                            : prefix + propertyName + ".";

                        if (propInfo.PropertyType == typeof(Type))
                            contextCollection.Properties.Add(newPrefix, value.ToString());
                        else if (propInfo.PropertyType == typeof(Assembly))
                            contextCollection.Properties.Add(newPrefix, value.ToString());
                        else if (propInfo.PropertyType.Namespace != null &&
                                 propInfo.PropertyType.Namespace.StartsWith("System.Reflection"))
                            contextCollection.Properties.Add(newPrefix, value.ToString());
                        else
                            ReflectObject(value, newPrefix, contextCollection, path);
                    }
                }
            }

            path.Remove(instance);
        }

        /// <summary>
        ///     Use reflection on a complex object to add it's values to our context collection.
        /// </summary>
        /// <param name="propertyName">Property that this collection belongs to</param>
        /// <param name="value"></param>
        /// <param name="contextCollection">Collection that values should be added to.</param>
        /// <param name="path">To prevent circular references.</param>
        protected void ReflectValue(string propertyName, object value, ContextCollectionDTO contextCollection,
            List<object> path)
        {
            if (_propertyFilter?.Invoke(propertyName, value) == true)
                return;
            if (IsFilteredOut(value))
                return;

            switch (value)
            {
                case null:
                    contextCollection.Properties.Add(propertyName, "null");
                    return;
                case string _:
                    contextCollection.Properties.Add(propertyName, value.ToString());
                    return;
                case Encoding enc:
                    contextCollection.Properties.Add(propertyName, enc.EncodingName);
                    return;
                case DateTimeFormatInfo v1:
                    contextCollection.Properties.Add(propertyName, v1.NativeCalendarName);
                    return;
                case CultureInfo v2:
                    contextCollection.Properties.Add(propertyName, "Culture[" + v2.LCID + "]");
                    return;
            }

            if (IsSimpleType(value.GetType()) || propertyName == "Encoding")
            {
                contextCollection.Properties.Add(propertyName, value.ToString());
            }
            else
            {
                var dictIf = GetGenericDictionaryInterface(value);
                var kvpTypes = GetKeyValuePairFromEnumeratorInterface(value);
                if (dictIf != null)
                {
                    _dictionaryConverterMethod.MakeGenericMethod(dictIf.GetGenericArguments())
                        .Invoke(this, new[] {propertyName, value, contextCollection, path});
                }
                else if (kvpTypes != null)
                {
                    _keyValuePairEnumeratorConverterMethod.MakeGenericMethod(kvpTypes)
                        .Invoke(this, new[] {propertyName, value, contextCollection, path});
                }

                else if (value is IDictionary)
                {
                    var items = value as IDictionary;
                    foreach (DictionaryEntry kvp in items)
                    {
                        var newPrefix = string.Format("{0}[{1}].", propertyName, kvp.Key);
                        ReflectObject(kvp.Value, newPrefix, contextCollection, path);
                    }
                }
                else if (value is IEnumerable items)
                {
                    var index = 0;
                    var sb = new StringBuilder("[");
                    foreach (var item in items)
                    {
                        if (item != null && IsSimpleType(item.GetType()))
                        {
                            sb.Append(item);
                            sb.Append(',');
                        }
                        else
                        {
                            var newPrefix = $"{propertyName}[{index}].";
                            ReflectObject(item, newPrefix, contextCollection, path);
                        }
                        index++;
                    }
                    if (sb.Length > 1)
                    {
                        sb.Remove(sb.Length - 1, 1);
                        sb.Append(']');
                        contextCollection.Properties.Add(propertyName, sb.ToString());
                    }
                }
                else
                {
                    var newPrefix = propertyName + ".";

                    if (value.GetType() == typeof(Type))
                        contextCollection.Properties.Add(newPrefix, value.ToString());
                    else if (value.GetType() == typeof(Assembly))
                        contextCollection.Properties.Add(newPrefix, value.ToString());
                    else if (value.GetType().Namespace != null &&
                             value.GetType().Namespace.StartsWith("System.Reflection"))
                        contextCollection.Properties.Add(newPrefix, value.ToString());
                    else
                        ReflectObject(value, newPrefix, contextCollection, path);
                }
            }


            path.Remove(value);
        }

        // ReSharper disable once UnusedMember.Local   //used through reflection
        private void ConvertDictionary<TKey, TValue>(string propertyName, IDictionary<TKey, TValue> value,
            ContextCollectionDTO contextCollection,
            List<object> path)
        {
            foreach (var kvp in value)
            {
                var key = kvp.Key == null ? "null" : kvp.Key.ToString();
                var prefix = string.IsNullOrEmpty(propertyName)
                    ? key
                    : $"{propertyName}[{key}]";
                ReflectValue(prefix, kvp.Value, contextCollection, path);
            }
        }


        private ContextCollectionDTO ConvertDictionaryToCollection(string collectionName, IDictionary dictionary)
        {
            var path = new List<object>();
            if (collectionName == "Dictionary`2")
                collectionName = "ContextData";
            var collection = new ContextCollectionDTO(collectionName);
            foreach (DictionaryEntry kvp in dictionary)
            {
                var propertyName = kvp.Key?.ToString() ?? "null";
                ReflectValue(propertyName, kvp.Value, collection, path);
            }
            return collection;
        }

        // ReSharper disable once UnusedMember.Local   //used through reflection
        private void ConvertKvpEnumerator<TKey, TValue>(string propertyName,
            IEnumerable<KeyValuePair<TKey, TValue>> enumerable, ContextCollectionDTO contextCollection,
            List<object> path)
        {
            var index = 0;
            foreach (var kvp in enumerable)
            {
                var key = kvp.Key == null ? "null" : kvp.Key.ToString();
                var prefix = string.IsNullOrEmpty(propertyName)
                    ? $"[{index++}]"
                    : $"{propertyName}[{index++}]";

                contextCollection.Properties.Add(prefix + ".Key", key);
                ReflectValue(prefix + ".Value", kvp.Value, contextCollection, path);
            }
        }

        private static Type GetGenericDictionaryInterface(object instance)
        {
            var dictIf = instance.GetType()
                .GetInterfaces()
                .FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IDictionary<,>));
            return dictIf;
        }

        private static Type[] GetKeyValuePairFromEnumeratorInterface(object instance)
        {
            // ReSharper disable once LoopCanBeConvertedToQuery - Easier to debug
            foreach (var @interface in instance.GetType().GetInterfaces())
            {
                if (!@interface.IsGenericType || !@interface.GetGenericArguments()[0].IsGenericType)
                    continue;

                var kvpType = @interface.GetGenericArguments()[0].GetGenericTypeDefinition();
                if (kvpType != typeof(KeyValuePair<,>))
                    continue;

                return @interface.GetGenericArguments()[0].GetGenericArguments();
            }
            return null;
        }

        private static bool IsFilteredOut(object instance)
        {
            if (instance is Exception)
                return false;

            var type = instance.GetType();
            if (type.FullName == "System.RuntimeType")
                return true;
            if (type.Name == "System.ContextStaticAttribute")
                return true;

            var ns = type.Namespace;
            if (ns == null) 
                return false;

            if (ns.StartsWith("System.Runtime"))
                return true;
            if (ns.StartsWith("System.Reflection"))
                return true;
            if (ns.StartsWith("System.Threading.Tasks"))
                return true;

            return false;
        }
    }
}