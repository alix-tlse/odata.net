﻿//---------------------------------------------------------------------
// <copyright file="BinaryDataScaleTests.cs" company="Microsoft">
//      Copyright (C) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.
// </copyright>
//---------------------------------------------------------------------

namespace Microsoft.OData.Performance
{
    using System;
    using System.IO;
    using Microsoft.OData;
    using Microsoft.OData.Edm;
    using BenchmarkDotNet.Attributes;

    /// <summary>
    /// Performance tests for reading and writing large binary data
    /// </summary>
    [MemoryDiagnoser]
    public class BinaryDataScaleTests : WriteReadFeedTestBase
    {
        private static IEdmModel ExchangeAttachmentModel = TestUtils.GetExchangeAttachmentModel();
        private static IEdmEntitySet TestEntitySet = ExchangeAttachmentModel.EntityContainer.FindEntitySet("Item");
        private static IEdmEntityType TestEntityType = ExchangeAttachmentModel.FindDeclaredType("PerformanceServices.Edm.ExchangeAttachment.Item") as IEdmEntityType;
        private const int NumberOfEntries = 10;
        private const int MaxStreamSize = 64 * 1024;
        private static Stream WriteStream = new MemoryStream(MaxStreamSize);


        [IterationSetup(Target = nameof(WriteFeedBinaryData_4MB))]
        public void SetupForWriteFeedBinaryData()
        {
            WriteStream.SetLength(0);
        }

        [Benchmark]
        public void WriteFeedBinaryData_4MB()
        {
            WriteFeed(WriteStream, ExchangeAttachmentModel, NumberOfEntries, Write4MB, TestEntitySet);
        }

        [GlobalSetup(Target = nameof(ReadFeedBinaryData_4MB))]
        public void SetupForReadFeedBinaryData()
        {
            WriteStream.SetLength(0);
            WriteFeed(WriteStream, ExchangeAttachmentModel, NumberOfEntries, Write4MB, TestEntitySet);
        }

        [Benchmark]
        public void ReadFeedBinaryData_4MB()
        {
            ReadFeed(WriteStream, ExchangeAttachmentModel, TestEntitySet, TestEntityType);
        }

        private void Write4MB(ODataWriter writer)
        {
            this.WriteEntry(writer, 4 * 1024);
        }

        private void WriteEntry(ODataWriter odataWriter, int dataSizeKb)
        {
            var entry = new ODataResource
            {
                Id = new Uri("http://www.odata.org/Perf.svc/Item(1)"),
                EditLink = new Uri("Item(1)", UriKind.Relative),
                ReadLink = new Uri("Item(1)", UriKind.Relative),
                TypeName = "PerformanceServices.Edm.ExchangeAttachment.Item",
                Properties = new[]
                    {
                        new ODataProperty{ Name = "HasAttachments", Value = false},
                    }
            };

            var attachmentsP = new ODataNestedResourceInfo(){Name = "Attachments", IsCollection = true};

            var attachmentsResourceSet = new ODataResourceSet()
            {
                TypeName = "Collection(PerformanceServices.Edm.ExchangeAttachment.Attachment)"
            };

            var attachment = dataSizeKb == 0 ? null
                : new ODataResource()
                {
                    TypeName = "PerformanceServices.Edm.ExchangeAttachment.Attachment",
                    Properties = new[]
                    {
                        new ODataProperty { Name = "Name", Value = "attachment" },
                        new ODataProperty { Name = "IsInline", Value = false },
                        new ODataProperty { Name = "LastModifiedTime", Value = new DateTimeOffset(1987, 6, 5, 4, 3, 21, 0, new TimeSpan(0, 0, 3, 0)) },
                        new ODataProperty { Name = "Content", Value = new byte[dataSizeKb * 1024]}, 
                    }
                };

            odataWriter.WriteStart(entry);
            odataWriter.WriteStart(attachmentsP);
            odataWriter.WriteStart(attachmentsResourceSet);
            if (attachment != null)
            {
                odataWriter.WriteStart(attachment);
                odataWriter.WriteEnd();
            }
            odataWriter.WriteEnd();
            odataWriter.WriteEnd();
            odataWriter.WriteEnd();
        }
    }
}
