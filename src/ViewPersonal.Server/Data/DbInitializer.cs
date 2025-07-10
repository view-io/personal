namespace ViewPersonal.Server.Data
{
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// Helper class to initialize the database
    /// </summary>
    public static class DbInitializer
    {
        /// <summary>
        /// Initializes the database by applying migrations and seeding initial data if needed
        /// </summary>
        /// <param name="app">The web application</param>
        public static void Initialize(WebApplication app)
        {
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var context = services.GetRequiredService<ApplicationDbContext>();
                    context.Database.Migrate();
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred while migrating or seeding the database.");
                }
            }
        }
    }
}