﻿using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using FluentAssertions;
using OneTrueError.Client.Contracts;
using Xunit;

namespace OneTrueError.Client.Tests
{
    public class ContextInfoTests
    {
        [Fact]
        public void Serialization_test()
        {
            var info = new ContextCollectionDTO("customer", new Dictionary<string, string>() {{"Key", "Value"}});
            DataContractSerializer serializer = new DataContractSerializer(typeof(ContextCollectionDTO));
            var ms = new MemoryStream();
            serializer.WriteObject(ms, info);
            ms.Position = 0;
            var obj = (ContextCollectionDTO)serializer.ReadObject(ms);

            obj.Name.Should().Be("customer");
        }
    }
}
