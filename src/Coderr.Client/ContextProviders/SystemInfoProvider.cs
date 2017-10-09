using System;
using System.Collections.Specialized;
using codeRR.Client.ContextProviders.Helpers;
using codeRR.Client.Contracts;
using codeRR.Client.Reporters;

namespace codeRR.Client.ContextProviders
{
    /// <summary>
    ///     Collects information about the computer (such as motherboard information). Will be added to a collection called
    ///     <c>SystemInfo</c>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Some fields are filtered out per default, look at the <see cref="Filter" /> property.
    ///     </para>
    /// </remarks>
    [DefaultProvider]
    public class SystemInfoProvider : IContextInfoProvider
    {
        /// <summary>
        ///     Fields which will be excluded.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Default is <c>"Caption", "DnsHostName", "Domain", "Name", "PrimaryOwnerName", "UserName", "Workgroup"</c>
        ///     </para>
        /// </remarks>
        public static string[] Filter =
        {
            "Caption", "DnsHostName", "Domain", "Name", "PrimaryOwnerName", "UserName",
            "Workgroup"
        };

        /// <summary>
        ///     Gets "SystemInfo"
        /// </summary>
        public string Name => "SystemInfo";

        /// <summary>
        ///     Collect information
        /// </summary>
        /// <param name="context">Contains information about the currently processed exception and where it came from.</param>
        /// <returns>
        ///     Collection
        /// </returns>
        public ContextCollectionDTO Collect(IErrorReporterContext context)
        {
            var contextCollection = new NameValueCollection();

            try
            {
                var collector = new ManagementCollector(contextCollection) {Filter = Filter};
                collector.Collect("Win32_ComputerSystem");
            }
            catch (Exception exception)
            {
                contextCollection.Add("CollectionException", exception.ToString());
            }

            return new ContextCollectionDTO("SystemInfo", contextCollection);
        }
    }
}