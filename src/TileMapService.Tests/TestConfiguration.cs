﻿namespace TileMapService.Tests
{
    /// <summary>
    /// Test environment configuration (port number, path to temporary files)
    /// </summary>
    internal static class TestConfiguration
    {
        public static int PortNumber => 5000;

        public static string BaseUrl => $"http://localhost:{PortNumber}";

        public static string DataPath => Path.Join(Path.GetTempPath(), "TileMapServiceTestData");
    }
}
