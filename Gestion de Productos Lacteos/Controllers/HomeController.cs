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

        // ?? GET /Home/Login ??????????????????????????????
        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetString("UsuarioNombre") != null)
                return RedirectToAction("Index");

            return View();
        }

        // ?? POST /Home/Login ?????????????????????????????
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

        // ?? GET /Home/Index ??????????????????????????????
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("UsuarioNombre") == null)
                return RedirectToAction("Login");

            ViewBag.Nombre = HttpContext.Session.GetString("UsuarioNombre");
            ViewBag.Rol = HttpContext.Session.GetString("UsuarioRol");

            return View();
        }

        // ?? GET /Home/Logout ?????????????????????????????
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}