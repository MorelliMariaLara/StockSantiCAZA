using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockSantiCaza.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Clientes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NombreRazonSocial = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    DniCuit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: true),
                    Telefono = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Domicilio = table.Column<string>(type: "nvarchar(220)", maxLength: 220, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clientes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Productos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Sku = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(800)", maxLength: 800, nullable: true),
                    Categoria = table.Column<int>(type: "int", nullable: false),
                    Marca = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Modelo = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Calibre = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    PrecioUnitario = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AlicuotaIva = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    StockActual = table.Column<int>(type: "int", nullable: false),
                    StockMinimo = table.Column<int>(type: "int", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Productos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CredencialesCLU",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    NumeroLegajo = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    FechaEmision = table.Column<DateOnly>(type: "date", nullable: false),
                    FechaVencimiento = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CredencialesCLU", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CredencialesCLU_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Ventas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    TipoComprobante = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    PuntoVenta = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    NumeroComprobante = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Cae = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    CaeVencimiento = table.Column<DateOnly>(type: "date", nullable: true),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DescuentoTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IvaTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ventas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ventas_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Armas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    NumeroSerie = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Marca = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Modelo = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Calibre = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    TipoArma = table.Column<int>(type: "int", nullable: false),
                    EstadoTramiteAnmac = table.Column<int>(type: "int", nullable: false),
                    NumeroTenenciaAnmac = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    ClienteActualId = table.Column<int>(type: "int", nullable: true),
                    FechaTransferencia = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Armas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Armas_Clientes_ClienteActualId",
                        column: x => x.ClienteActualId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Armas_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MunicionLotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    NumeroLote = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Calibre = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    TipoMunicion = table.Column<int>(type: "int", nullable: false),
                    CantidadDisponible = table.Column<int>(type: "int", nullable: false),
                    FechaIngreso = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaVencimiento = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MunicionLotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MunicionLotes_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DetallesVenta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VentaId = table.Column<int>(type: "int", nullable: false),
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    ArmaId = table.Column<int>(type: "int", nullable: true),
                    MunicionLoteId = table.Column<int>(type: "int", nullable: true),
                    Cantidad = table.Column<int>(type: "int", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Descuento = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AlicuotaIva = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Iva = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetallesVenta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DetallesVenta_Armas_ArmaId",
                        column: x => x.ArmaId,
                        principalTable: "Armas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DetallesVenta_MunicionLotes_MunicionLoteId",
                        column: x => x.MunicionLoteId,
                        principalTable: "MunicionLotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DetallesVenta_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DetallesVenta_Ventas_VentaId",
                        column: x => x.VentaId,
                        principalTable: "Ventas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MovimientosStock",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    VentaId = table.Column<int>(type: "int", nullable: true),
                    ArmaId = table.Column<int>(type: "int", nullable: true),
                    MunicionLoteId = table.Column<int>(type: "int", nullable: true),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Cantidad = table.Column<int>(type: "int", nullable: false),
                    StockResultante = table.Column<int>(type: "int", nullable: false),
                    Observacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimientosStock", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MovimientosStock_Armas_ArmaId",
                        column: x => x.ArmaId,
                        principalTable: "Armas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovimientosStock_MunicionLotes_MunicionLoteId",
                        column: x => x.MunicionLoteId,
                        principalTable: "MunicionLotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovimientosStock_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovimientosStock_Ventas_VentaId",
                        column: x => x.VentaId,
                        principalTable: "Ventas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Armas_ClienteActualId",
                table: "Armas",
                column: "ClienteActualId");

            migrationBuilder.CreateIndex(
                name: "IX_Armas_NumeroSerie",
                table: "Armas",
                column: "NumeroSerie",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Armas_ProductoId",
                table: "Armas",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_DniCuit",
                table: "Clientes",
                column: "DniCuit",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CredencialesCLU_ClienteId",
                table: "CredencialesCLU",
                column: "ClienteId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CredencialesCLU_NumeroLegajo",
                table: "CredencialesCLU",
                column: "NumeroLegajo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DetallesVenta_ArmaId",
                table: "DetallesVenta",
                column: "ArmaId");

            migrationBuilder.CreateIndex(
                name: "IX_DetallesVenta_MunicionLoteId",
                table: "DetallesVenta",
                column: "MunicionLoteId");

            migrationBuilder.CreateIndex(
                name: "IX_DetallesVenta_ProductoId",
                table: "DetallesVenta",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_DetallesVenta_VentaId",
                table: "DetallesVenta",
                column: "VentaId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosStock_ArmaId",
                table: "MovimientosStock",
                column: "ArmaId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosStock_MunicionLoteId",
                table: "MovimientosStock",
                column: "MunicionLoteId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosStock_ProductoId",
                table: "MovimientosStock",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosStock_VentaId",
                table: "MovimientosStock",
                column: "VentaId");

            migrationBuilder.CreateIndex(
                name: "IX_MunicionLotes_ProductoId_NumeroLote",
                table: "MunicionLotes",
                columns: new[] { "ProductoId", "NumeroLote" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Productos_Sku",
                table: "Productos",
                column: "Sku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_ClienteId",
                table: "Ventas",
                column: "ClienteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CredencialesCLU");

            migrationBuilder.DropTable(
                name: "DetallesVenta");

            migrationBuilder.DropTable(
                name: "MovimientosStock");

            migrationBuilder.DropTable(
                name: "Armas");

            migrationBuilder.DropTable(
                name: "MunicionLotes");

            migrationBuilder.DropTable(
                name: "Ventas");

            migrationBuilder.DropTable(
                name: "Productos");

            migrationBuilder.DropTable(
                name: "Clientes");
        }
    }
}
