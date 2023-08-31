﻿namespace TileMapService
{
    /// <summary>
    /// Media type identifiers.
    /// </summary>
    /// <remarks>
    /// Structure is similar to <see cref="System.Net.Mime.MediaTypeNames"/> class.
    /// </remarks>
    public static class MediaTypeNames
    {
        public static class Image
        {
            public const string Png = "image/png";

            public const string Jpeg = "image/jpeg"; 
            
            public const string Tiff = "image/tiff";

            public const string Webp = "image/webp";
        }

        public static class Text
        {
            public const string Xml = "text/xml";
            
            public const string XmlUtf8 = "text/xml; charset=utf-8"; // TODO: better way?

            public const string Plain = "text/plain";
        }

        public static class Application
        {
            public const string Xml = "application/xml";

            public const string MapboxVectorTile = "application/vnd.mapbox-vector-tile";

            public const string XProtobuf = "application/x-protobuf";

            public const string OgcServiceExceptionXml = "application/vnd.ogc.se_xml";

            public const string OgcWmsCapabilitiesXml = "application/vnd.ogc.wms_xml";
        }
    }
}
