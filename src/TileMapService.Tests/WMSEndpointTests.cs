﻿using System.Net;
using System.Text.Json;
using System.Xml;

using Microsoft.Extensions.Hosting;
using NUnit.Framework;

using MBT = TileMapService.MBTiles;

namespace TileMapService.Tests
{
    /// <summary>
    /// End-to-end tests for Web Map Service (WMS) endpoint.
    /// </summary>
    [TestFixture]
    public class WMSEndpointTests
    {
        private IHost serviceHost;

        private HttpClient client;

        private static string MbtilesFilePath1 => Path.Join(TestConfiguration.DataPath, "test-wms-world-mercator-hd.mbtiles");

        private static string MbtilesFilePath2 => Path.Join(TestConfiguration.DataPath, "test-wms-small-area.mbtiles");

        // TODO: EPSG:4326 support private static string LocalFilesPath => Path.Join(TestConfiguration.DataPath, "test-wms-world-geodetic");

        [OneTimeSetUp]
        public async Task Setup()
        {
            RemoveTestData();
            await PrepareTestDataAsync();

            var tileSources = new[]
            {
                new SourceConfiguration
                {
                    Type = SourceConfiguration.TypeMBTiles,
                    Id = "world-mercator-hd",
                    Location = MbtilesFilePath1,
                },
                new SourceConfiguration
                {
                    Type = SourceConfiguration.TypeMBTiles,
                    Id = "small-area",
                    Location = MbtilesFilePath2,
                },
                ////new SourceConfiguration
                ////{
                ////    Type = SourceConfiguration.TypeLocalFiles,
                ////    Title = "World Geodetic EPSG:4326",
                ////    Id = "world-geodetic",
                ////    Location = LocalFilesPath + "\\{z}\\{x}\\{y}.jpg",
                ////    Srs = Utils.SrsCodes.EPSG4326,
                ////    Format = ImageFormats.Jpeg,
                ////    MinZoom = 0,
                ////    MaxZoom = 3,
                ////},
            };

            this.client = new HttpClient
            {
                BaseAddress = new Uri(TestConfiguration.BaseUrl),
            };

            var json = JsonSerializer.Serialize(
                new
                {
                    Sources = tileSources,
                    Service = new { keywords = "wms,service,tile" }
                });

            this.serviceHost = await TestsUtility.CreateAndRunServiceHostAsync(json, TestConfiguration.PortNumber);

            // TODO: proxy source for WMS
        }

        [OneTimeTearDown]
        public async Task TearDown()
        {
            await this.serviceHost.StopAsync();
            this.serviceHost.Dispose();

            // Skip deleting files to analyze them
            if (!System.Diagnostics.Debugger.IsAttached)
            {
                RemoveTestData();
            }
        }

        [Test]
        public async Task GetWmsCapabilitiesAsync()
        {
            // 1. Service
            var r = await client.GetAsync("/wms" + "?service=WMS&request=GetCapabilities");
            Assert.AreEqual(HttpStatusCode.OK, r.StatusCode);
            var wmtsXml = await r.Content.ReadAsStringAsync();
            var xml = new XmlDocument();
            xml.LoadXml(wmtsXml);

            var attributes = xml.SelectSingleNode("/WMT_MS_Capabilities").Attributes;
            Assert.AreEqual("1.1.1", attributes["version"].Value);

            // 2. Service properties
            var keywords = xml.SelectNodes("/WMT_MS_Capabilities/Service/KeywordList/Keyword");
            Assert.AreEqual(3, keywords.Count);

            // 3. Layers (Sources)
            var layers = xml.SelectNodes("/WMT_MS_Capabilities/Capability/Layer");
            Assert.AreEqual(2, layers.Count);
        }

        [Test]
        public async Task GetWmsCapabilitiesInvalidParameterValueErrorAsync()
        {
            var r = await client.GetAsync("/wms" + "?service=QWERTY&request=GetCapabilities");
            Assert.AreEqual(HttpStatusCode.OK, r.StatusCode);
            var wmsXml = await r.Content.ReadAsStringAsync();

            var xml = new XmlDocument();
            xml.LoadXml(wmsXml);
            var nsManager = new XmlNamespaceManager(xml.NameTable);
            nsManager.AddNamespace("ows", "http://www.opengis.net/ows");

            var messageNode = xml.SelectSingleNode("/ows:ExceptionReport/ows:Exception", nsManager);
            Assert.IsNotNull(messageNode);
            Assert.IsTrue(messageNode.InnerText.Length > 10);
            Assert.AreEqual("InvalidParameterValue", messageNode.Attributes["exceptionCode"].Value);
        }

        [Test]
        public async Task GetWmsCapabilitiesOperationNotSupportedErrorAsync()
        {
            var r = await client.GetAsync("/wms" + "?service=WMS&request=QUERTY");
            Assert.AreEqual(HttpStatusCode.OK, r.StatusCode);
            var wmsXml = await r.Content.ReadAsStringAsync();

            var xml = new XmlDocument();
            xml.LoadXml(wmsXml);
            var nsManager = new XmlNamespaceManager(xml.NameTable);
            nsManager.AddNamespace("ogc", "http://www.opengis.net/ogc");

            var messageNode = xml.SelectSingleNode("/ogc:ServiceExceptionReport/ogc:ServiceException", nsManager);
            Assert.IsNotNull(messageNode);
            Assert.IsTrue(messageNode.InnerText.Length > 10);
            Assert.AreEqual("OperationNotSupported", messageNode.Attributes["code"].Value);
        }

        [Test]
        public async Task GetMapWebMercatorEntireWorldAsync()
        {
            var r = await client.GetAsync("/wms/?SERVICE=WMS&REQUEST=GetMap&LAYERS=world-mercator-hd&FORMAT=image/png&SRS=EPSG:3857&WIDTH=1024&HEIGHT=1024&BBOX=-20026376.39,-20048966.10,20026376.39,20048966.10");
            Assert.AreEqual(HttpStatusCode.OK, r.StatusCode);
            Assert.AreEqual("image/png", r.Content.Headers.ContentType.MediaType);
            var actual = await r.Content.ReadAsByteArrayAsync();
            Assert.IsTrue(actual.Length > 1000);

            var size = Utils.ImageHelper.GetImageSize(actual);
            Assert.AreEqual(1024, size.Value.Width);
            Assert.AreEqual(1024, size.Value.Height);

            var color = Utils.ImageHelper.CheckIfImageIsBlank(actual);
            Assert.IsNotNull(color);
            Assert.AreEqual(0xFF0000FF, color.Value); // TODO: check choosen Z=2 for rendering entire world map
        }

        private static Task PrepareTestDataAsync()
        {
            if (!Directory.Exists(TestConfiguration.DataPath))
            {
                Directory.CreateDirectory(TestConfiguration.DataPath);
            }

            // 1. MBTiles database 1
            const int HDTileSize = 512;
            var db1 = MBT.Repository.CreateEmptyDatabase(MbtilesFilePath1);
            db1.AddMetadataItem(new MBT.MetadataItem(MBT.MetadataItem.KeyName, "World Mercator HD"));
            db1.AddMetadataItem(new MBT.MetadataItem(MBT.MetadataItem.KeyFormat, ImageFormats.Png));
            db1.AddTile(0, 0, 0, Utils.ImageHelper.CreateEmptyImage(HDTileSize, HDTileSize, 0xFFFF0000, SkiaSharp.SKEncodedImageFormat.Png, 0));
            db1.AddTile(0, 0, 1, Utils.ImageHelper.CreateEmptyImage(HDTileSize, HDTileSize, 0xFF00FF00, SkiaSharp.SKEncodedImageFormat.Png, 0));
            for (var x = 0; x < 4; x++)
            {
                for (var y = 0; y < 4; y++)
                {
                    db1.AddTile(x, y, 2, Utils.ImageHelper.CreateEmptyImage(HDTileSize, HDTileSize, 0xFF0000FF, SkiaSharp.SKEncodedImageFormat.Png, 0));
                }
            }

            // 2. MBTiles database 2
            const int TileSize = 256;
            var db2 = MBT.Repository.CreateEmptyDatabase(MbtilesFilePath2);
            db2.AddMetadataItem(new MBT.MetadataItem(MBT.MetadataItem.KeyName, "Small Area"));
            db2.AddMetadataItem(new MBT.MetadataItem(MBT.MetadataItem.KeyFormat, ImageFormats.Jpeg));
            db2.AddTile(0, 0, 8, Utils.ImageHelper.CreateEmptyImage(TileSize, TileSize, 0xFFFF0000, SkiaSharp.SKEncodedImageFormat.Jpeg, 85));
            db2.AddTile(0, 0, 10, Utils.ImageHelper.CreateEmptyImage(TileSize, TileSize, 0xFF00FF00, SkiaSharp.SKEncodedImageFormat.Jpeg, 85));

            return Task.CompletedTask;
        }

        private static void RemoveTestData()
        {
            if (Directory.Exists(TestConfiguration.DataPath))
            {
                if (File.Exists(MbtilesFilePath1))
                {
                    File.Delete(MbtilesFilePath1);
                }

                if (File.Exists(MbtilesFilePath2))
                {
                    File.Delete(MbtilesFilePath2);
                }
            }
        }
    }
}
