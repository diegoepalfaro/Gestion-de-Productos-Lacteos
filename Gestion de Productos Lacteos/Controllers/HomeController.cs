using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace Gestion_de_Productos_Lacteos.Controllers
{
    public class HomeController : Controller
    {
        private readonly string _connectionString;

        public HomeController(IConfiguration configuration)
        {
            _connectionString = configuration
                .GetConnectionString("GestionProductosLacteosDbConnection");
        }

        // ── GET /Home/Login ──────────────────────────────
        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetString("UsuarioNombre") != null)
                return RedirectToAction("Index");
            return View();
        }

        // ── POST /Home/Login ─────────────────────────────
        [HttpPost]
        public IActionResult Login(string correo, string contraseña)
        {
            if (string.IsNullOrEmpty(correo) || string.IsNullOrEmpty(contraseña))
            {
                ViewBag.Error = "El correo y la contraseña son obligatorios";
                return View();
            }

            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            string query = @"SELECT u.idUsuario, u.nombre, u.usuario, r.nombreRol
                             FROM Usuario u
                             INNER JOIN Rol r ON u.idRol = r.idRol
                             WHERE u.correo     = @correo
                               AND u.contraseña = @contrasena
                               AND u.estado     = 1";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@correo", correo.Trim());
            cmd.Parameters.AddWithValue("@contrasena", contraseña.Trim());
            using var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                HttpContext.Session.SetInt32("UsuarioId", reader.GetInt32(0));
                HttpContext.Session.SetString("UsuarioNombre", reader.GetString(1));
                HttpContext.Session.SetString("UsuarioAlias", reader.GetString(2));
                HttpContext.Session.SetString("UsuarioRol", reader.GetString(3));
                return RedirectToAction("Index");
            }

            ViewBag.Error = "Correo o contraseña incorrectos";
            return View();
        }

        // ── GET /Home/Index ──────────────────────────────
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("UsuarioNombre") == null)
                return RedirectToAction("Login");

            ViewBag.Nombre = HttpContext.Session.GetString("UsuarioNombre");
            ViewBag.Rol = HttpContext.Session.GetString("UsuarioRol");
            return View();
        }

        // ── GET /Home/Dashboard ──────────────────────────
        public IActionResult Dashboard()
        {
            if (HttpContext.Session.GetString("UsuarioNombre") == null)
                return RedirectToAction("Login");

            ViewBag.Nombre = HttpContext.Session.GetString("UsuarioNombre");
            ViewBag.Rol = HttpContext.Session.GetString("UsuarioRol");

            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using (var cmd = new SqlCommand(@"
                SELECT ISNULL(SUM(total),0) FROM Venta
                WHERE CAST(fechaVenta AS DATE) = CAST(GETDATE() AS DATE)", conn))
                ViewBag.VentasHoy = cmd.ExecuteScalar();

            using (var cmd = new SqlCommand(@"
                SELECT COUNT(*) FROM Venta
                WHERE CAST(fechaVenta AS DATE) = CAST(GETDATE() AS DATE)", conn))
                ViewBag.NumVentasHoy = cmd.ExecuteScalar();

            using (var cmd = new SqlCommand(@"
                SELECT ISNULL(SUM(total),0) FROM Venta
                WHERE MONTH(fechaVenta)=MONTH(GETDATE())
                  AND YEAR(fechaVenta)=YEAR(GETDATE())", conn))
                ViewBag.VentasMes = cmd.ExecuteScalar();

            using (var cmd = new SqlCommand(@"
                SELECT COUNT(*) FROM Lote
                WHERE fechaVencimiento >= CAST(GETDATE() AS DATE)
                  AND fechaVencimiento <= DATEADD(DAY,7,CAST(GETDATE() AS DATE))
                  AND cantidad > 0", conn))
                ViewBag.PorVencer = cmd.ExecuteScalar();

            using (var cmd = new SqlCommand(@"
                SELECT COUNT(*) FROM Lote
                WHERE fechaVencimiento < CAST(GETDATE() AS DATE)
                  AND cantidad > 0", conn))
                ViewBag.Vencidos = cmd.ExecuteScalar();

            using (var cmd = new SqlCommand(@"
                SELECT COUNT(*) FROM Inventario
                WHERE stockActual <= stockMinimo", conn))
                ViewBag.StockBajo = cmd.ExecuteScalar();

            // Lotes por vencer
            var lotesPorVencer = new List<dynamic>();
            using (var cmd = new SqlCommand(@"
                SELECT p.nombreProducto, l.numeroLote, l.fechaVencimiento,
                       l.cantidad, DATEDIFF(DAY,GETDATE(),l.fechaVencimiento) AS dias
                FROM Lote l INNER JOIN Producto p ON l.idProducto=p.idProducto
                WHERE l.fechaVencimiento >= CAST(GETDATE() AS DATE)
                  AND l.fechaVencimiento <= DATEADD(DAY,7,CAST(GETDATE() AS DATE))
                  AND l.cantidad > 0
                ORDER BY l.fechaVencimiento ASC", conn))
            using (var r = cmd.ExecuteReader())
                while (r.Read())
                    lotesPorVencer.Add(new
                    {
                        Producto = r.GetString(0),
                        NumeroLote = r.GetString(1),
                        FechaVence = r.GetDateTime(2).ToString("dd/MM/yyyy"),
                        Cantidad = r.GetInt32(3),
                        DiasRestantes = r.GetInt32(4)
                    });
            ViewBag.LotesPorVencer = lotesPorVencer;

            // Lotes vencidos
            var lotesVencidos = new List<dynamic>();
            using (var cmd = new SqlCommand(@"
                SELECT p.nombreProducto, l.numeroLote, l.fechaVencimiento,
                       l.cantidad, DATEDIFF(DAY,l.fechaVencimiento,GETDATE()) AS dias
                FROM Lote l INNER JOIN Producto p ON l.idProducto=p.idProducto
                WHERE l.fechaVencimiento < CAST(GETDATE() AS DATE)
                  AND l.cantidad > 0
                ORDER BY l.fechaVencimiento DESC", conn))
            using (var r = cmd.ExecuteReader())
                while (r.Read())
                    lotesVencidos.Add(new
                    {
                        Producto = r.GetString(0),
                        NumeroLote = r.GetString(1),
                        FechaVence = r.GetDateTime(2).ToString("dd/MM/yyyy"),
                        Cantidad = r.GetInt32(3),
                        DiasVencido = r.GetInt32(4)
                    });
            ViewBag.LotesVencidos = lotesVencidos;

            // Últimas ventas
            var ultimasVentas = new List<dynamic>();
            using (var cmd = new SqlCommand(@"
                SELECT TOP 5 v.idVenta, c.nombre, v.tipoComprobante,
                       v.total, v.fechaVenta
                FROM Venta v INNER JOIN Cliente c ON v.idCliente=c.idCliente
                WHERE CAST(v.fechaVenta AS DATE) = CAST(GETDATE() AS DATE)
                ORDER BY v.fechaVenta DESC", conn))
            using (var r = cmd.ExecuteReader())
                while (r.Read())
                    ultimasVentas.Add(new
                    {
                        IdVenta = r.GetInt32(0),
                        Cliente = r.GetString(1),
                        TipoComprobante = r.IsDBNull(2) ? "-" : r.GetString(2),
                        Total = r.GetDecimal(3),
                        Fecha = r.GetDateTime(4).ToString("hh:mm tt")
                    });
            ViewBag.UltimasVentas = ultimasVentas;

            return View();
        }

        // ── GET /Home/Logout ─────────────────────────────
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}