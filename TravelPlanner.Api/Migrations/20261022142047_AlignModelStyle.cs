using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelPlanner.Api.Migrations
{
    /// <inheritdoc />
    public partial class AlignModelStyle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Изменены имена свойств C#, столбцы БД сохранены через атрибуты Column.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Схема БД не менялась.
        }
    }
}
