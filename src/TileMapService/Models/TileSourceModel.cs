namespace TileMapService.Models;

public record TileSourceModel(string Type,
                              string Id,
                              string? Format,
                              string Title,
                              string Abstraction,
                              bool? Tms,
                              int? MinZoom,
                              int? MaxZoom,
                              string? Srs);
