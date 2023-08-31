﻿using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace TileMapService.TileSources
{
    /// <summary>
    /// Represents tile source with tiles stored in MBTiles file (SQLite database).
    /// </summary>
    /// <remarks>
    /// Supports only Spherical Mercator (EPSG:3857) tile grid and TMS tiling scheme (Y axis is going up).
    /// See MBTiles 1.3 specification: https://github.com/mapbox/mbtiles-spec/blob/master/1.3/spec.md
    /// </remarks>
    class MBTilesTileSource : ITileSource
    {
        private SourceConfiguration configuration;

        private MBTiles.Repository? repository;

        public MBTilesTileSource(SourceConfiguration configuration)
        {
            if (String.IsNullOrEmpty(configuration.Id))
            {
                throw new ArgumentException("Source identifier is null or empty string");
            }

            if (String.IsNullOrEmpty(configuration.Location))
            {
                throw new ArgumentException("Source location is null or empty string");
            }

            this.configuration = configuration; // Will be changed later in InitAsync
        }

        #region ITileSource implementation

        Task ITileSource.InitAsync()
        {
            // Configuration values priority:
            // 1. Default values for MBTiles source type.
            // 2. Actual values (MBTiles metadata table values).
            // 3. Values from configuration file - overrides given above, if provided.

            if (String.IsNullOrEmpty(this.configuration.Location))
            {
                throw new InvalidOperationException("configuration.Location is null or empty");
            }

            this.repository = new MBTiles.Repository(configuration.Location, false);
            var metadata = new MBTiles.Metadata(this.repository.ReadMetadata());

            var title = String.IsNullOrEmpty(this.configuration.Title) ?
                    (!String.IsNullOrEmpty(metadata.Name) ? metadata.Name : this.configuration.Id) :
                    this.configuration.Title;

            var format = String.IsNullOrEmpty(this.configuration.Format) ?
                    (!String.IsNullOrEmpty(metadata.Format) ? metadata.Format : ImageFormats.Png) :
                    this.configuration.Format;

            // Get tile width and height from first tile, for raster formats
            var tileWidth = Utils.WebMercator.DefaultTileWidth;
            var tileHeight = Utils.WebMercator.DefaultTileHeight;
            var firstTile = this.repository.ReadFirstTile();
            if (firstTile != null && (format == ImageFormats.Png || format == ImageFormats.Jpeg))
            {
                var size = Utils.ImageHelper.GetImageSize(firstTile);
                if (size.HasValue)
                {
                    tileWidth = size.Value.Width;
                    tileHeight = size.Value.Height;
                }
            }

            var minZoom = this.configuration.MinZoom ?? metadata.MinZoom ?? null;
            var maxZoom = this.configuration.MaxZoom ?? metadata.MaxZoom ?? null;
            if (minZoom == null || maxZoom == null)
            {
                var zoomRange = this.repository.GetZoomLevelRange();
                if (zoomRange.HasValue)
                {
                    minZoom = zoomRange.Value.Min;
                    maxZoom = zoomRange.Value.Max;
                }
                else
                {
                    minZoom = 0;
                    maxZoom = 20;
                }
            }

            // Re-create configuration
            this.configuration = new SourceConfiguration
            {
                Id = this.configuration.Id,
                Type = this.configuration.Type,
                Format = format,
                Title = title,
                Abstract = this.configuration.Abstract,
                Attribution = String.IsNullOrEmpty(this.configuration.Attribution) ? metadata.Attribution : null,
                Tms = this.configuration.Tms ?? true, // Default true for the MBTiles, following the Tile Map Service Specification.
                Srs = Utils.SrsCodes.EPSG3857, // MBTiles supports only Spherical Mercator tile grid
                Location = this.configuration.Location,
                ContentType = Utils.EntitiesConverter.TileFormatToContentType(format),
                MinZoom = minZoom,
                MaxZoom = maxZoom,
                GeographicalBounds = metadata.Bounds, // Can be null, if no corresponding record in 'metadata' table
                TileWidth = tileWidth,
                TileHeight = tileHeight,
                Cache = null, // Not used for MBTiles source
            };

            return Task.CompletedTask;
        }

        Task<byte[]?> ITileSource.GetTileAsync(int x, int y, int z)
        {
            if (this.repository == null)
            {
                throw new InvalidOperationException("Repository was not initialized.");
            }

            var tileRow = this.configuration.Tms != null && this.configuration.Tms.Value ? y : Utils.WebMercator.FlipYCoordinate(y, z);
            var tileData = this.repository.ReadTile(x, tileRow, z);

            // TODO: pass gzipped data as-is with setting HTTP headers?
            // pbf as a format refers to gzip-compressed vector tile data in Mapbox Vector Tile format, 
            // which uses Google Protocol Buffers as encoding format.
            if (this.configuration.Format == ImageFormats.Protobuf && tileData != null)
            {
                using var compressedStream = new MemoryStream(tileData);
                using var zipStream = new GZipStream(compressedStream, CompressionMode.Decompress);
                using var resultStream = new MemoryStream();
                zipStream.CopyTo(resultStream);
                tileData = resultStream.ToArray();
            }

            return Task.FromResult(tileData);
        }

        SourceConfiguration ITileSource.Configuration => this.configuration;

        #endregion
    }
}
