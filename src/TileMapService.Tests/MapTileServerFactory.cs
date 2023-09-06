using Microsoft.AspNetCore.Mvc.Testing;

namespace TileMapService.Tests;
public class MapTileServerFactory : WebApplicationFactory<TileMapService.Program>
{
    private static MapTileServerFactory instance = default!;
    private static readonly object _lock = new();

    public static MapTileServerFactory Instance
    {
        get
        {
            if (instance == null)
            {
                lock (_lock)
                {
                    instance ??= new MapTileServerFactory();
                }
            }
            return instance;
        }
    }
}
