using System.Diagnostics;

namespace TileMapService
{
    class ErrorLoggingMiddleware
    {
        private readonly RequestDelegate next;

        public ErrorLoggingMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await this.next(context);
            }
            catch (Exception e)
            {
                Debug.WriteLine($"The following error happened: {e.Message}");
                throw;
            }
        }
    }
}
