using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;

class Program
{
    private static DatabaseHelper dbHelper;
    private static Usuario usuarioActual;

    static void Main(string[] args)
    {
        // Configurar la conexión a la base de datos
        string connectionString = "Server=DESKTOP-4HF687T;Database=GestionDenunciasFraude;Integrated Security=True;";
        dbHelper = new DatabaseHelper(connectionString);

        // Mostrar pantalla de login
        MostrarLogin();

        // Si el login es exitoso, mostrar el menú principal
        if (usuarioActual != null)
        {
            MostrarMenuPrincipal();
        }
    }

    static void MostrarLogin()
    {
        Console.Clear();
        Console.WriteLine("=== SISTEMA DE GESTIÓN DE DENUNCIAS DE FRAUDE ===");
        Console.WriteLine("Ingrese sus credenciales");

        Console.Write("Email: ");
        string email = Console.ReadLine();

        Console.Write("Contraseña: ");
        string password = Console.ReadLine();

        usuarioActual = dbHelper.Login(email, password);

        if (usuarioActual == null)
        {
            Console.WriteLine("Credenciales incorrectas. Presione cualquier tecla para intentar nuevamente...");
            Console.ReadKey();
            MostrarLogin();
        }
    }

    static void MostrarMenuPrincipal()
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine($"=== SISTEMA DE GESTIÓN DE DENUNCIAS DE FRAUDE === (Usuario: {usuarioActual.Nombre} - {usuarioActual.Rol})");
            Console.WriteLine("1. Registrar nueva denuncia");
            Console.WriteLine("2. Ver listado de denuncias");
            Console.WriteLine("3. Buscar denuncias");
            Console.WriteLine("4. Generar reportes estadísticos");

            if (usuarioActual.Rol == "Admin")
            {
                Console.WriteLine("5. Administrar usuarios");
            }

            Console.WriteLine("0. Salir");
            Console.Write("Seleccione una opción: ");

            string opcion = Console.ReadLine();

            switch (opcion)
            {
                case "1":
                    RegistrarNuevaDenuncia();
                    break;
                case "2":
                    VerListadoDenuncias();
                    break;
                case "3":
                    BuscarDenuncias();
                    break;
                case "4":
                    GenerarReportes();
                    break;
                case "5" when usuarioActual.Rol == "Admin":
                    AdministrarUsuarios();
                    break;
                case "0":
                    return;
                default:
                    Console.WriteLine("Opción no válida. Presione cualquier tecla para continuar...");
                    Console.ReadKey();
                    break;
            }
        }
    }

    static void RegistrarNuevaDenuncia()
    {
        Console.Clear();
        Console.WriteLine("=== REGISTRAR NUEVA DENUNCIA ===");

        Denuncia denuncia = new Denuncia
        {
            FechaRegistro = DateTime.Now,
            UsuarioCreadorId = usuarioActual.Id,
            Estado = "No asignado"
        };

        // Solicitar datos básicos
        denuncia.NumeroDenuncia = GenerarNumeroDenuncia();
        Console.WriteLine($"Número de denuncia: {denuncia.NumeroDenuncia}");

        Console.Write("Fecha del evento (dd/mm/aaaa): ");
        denuncia.FechaEvento = DateTime.Parse(Console.ReadLine());

        Console.Write("Tipo de delito: ");
        denuncia.TipoDelito = Console.ReadLine();

        Console.Write("Dotación policial (opcional): ");
        denuncia.DotacionPolicial = Console.ReadLine();

        Console.Write("Provincia: ");
        denuncia.Provincia = Console.ReadLine();

        Console.Write("Municipio: ");
        denuncia.Municipio = Console.ReadLine();

        // Datos de la víctima
        Console.WriteLine("\nDATOS DE LA VÍCTIMA");
        Console.Write("Nombre completo: ");
        denuncia.NombreVictima = Console.ReadLine();

        Console.Write("Cédula: ");
        denuncia.CedulaVictima = Console.ReadLine();

        Console.WriteLine("Teléfonos (ingrese uno por línea, vacío para terminar):");
        string telefono;
        do
        {
            telefono = Console.ReadLine();
            if (!string.IsNullOrEmpty(telefono))
            {
                denuncia.TelefonosVictima.Add(telefono);
            }
        } while (!string.IsNullOrEmpty(telefono));

        // Datos del beneficiario (opcional)
        Console.WriteLine("\nDATOS DEL BENEFICIARIO (SOSPECHOSO)");
        Console.Write("Nombre completo (opcional): ");
        denuncia.NombreBeneficiario = Console.ReadLine();

        if (!string.IsNullOrEmpty(denuncia.NombreBeneficiario))
        {
            Console.Write("Cédula (opcional): ");
            denuncia.CedulaBeneficiario = Console.ReadLine();

            Console.WriteLine("Teléfonos (ingrese uno por línea, vacío para terminar):");
            do
            {
                telefono = Console.ReadLine();
                if (!string.IsNullOrEmpty(telefono))
                {
                    if (denuncia.TelefonosBeneficiario == null)
                        denuncia.TelefonosBeneficiario = new List<string>();

                    denuncia.TelefonosBeneficiario.Add(telefono);
                }
            } while (!string.IsNullOrEmpty(telefono));

            Console.Write("Monto del fraude (opcional): ");
            string montoStr = Console.ReadLine();
            if (!string.IsNullOrEmpty(montoStr))
            {
                denuncia.MontoFraude = decimal.Parse(montoStr);
            }

            // Cuentas bancarias
            Console.WriteLine("\nCUENTAS BANCARIAS (opcional)");
            Console.WriteLine("Ingrese cuentas en formato 'Banco,Número' (vacío para terminar):");
            string cuentaStr;
            do
            {
                cuentaStr = Console.ReadLine();
                if (!string.IsNullOrEmpty(cuentaStr))
                {
                    var partes = cuentaStr.Split(',');
                    if (partes.Length >= 2)
                    {
                        if (denuncia.CuentasBancarias == null)
                            denuncia.CuentasBancarias = new List<CuentaBancaria>();

                        denuncia.CuentasBancarias.Add(new CuentaBancaria
                        {
                            Banco = partes[0].Trim(),
                            NumeroCuenta = partes[1].Trim()
                        });
                    }
                }
            } while (!string.IsNullOrEmpty(cuentaStr));
        }

        // Procedencia de la denuncia
        Console.WriteLine("\nPROCEDENCIA DE LA DENUNCIA");
        Console.WriteLine("1. Policía Nacional");
        Console.WriteLine("2. Ministerio Público");
        Console.Write("Seleccione: ");
        string procedencia = Console.ReadLine();
        denuncia.ProcedenciaDenuncia = procedencia == "1" ? "Policía Nacional" : "Ministerio Público";

        // PDF adjunto
        Console.Write("\n¿Desea cambiar el PDF adjunto? (s/n): ");
        if (Console.ReadLine().ToLower() == "s")
        {
            bool pdfValido = false;
            byte[] pdfBytes = null;

            while (!pdfValido)
            {
                Console.Write("Ruta del nuevo archivo PDF (vacío para eliminar actual): ");
                string filePath = Console.ReadLine();

                if (string.IsNullOrEmpty(filePath))
                {
                    pdfValido = true;
                    pdfBytes = null;
                    break;
                }

                try
                {
                    if (File.Exists(filePath))
                    {
                        // Verificaciones como en el método anterior
                        if (!filePath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                        {
                            Console.WriteLine("ERROR: El archivo debe tener extensión .PDF");
                            continue;
                        }

                        FileInfo fileInfo = new FileInfo(filePath);
                        if (fileInfo.Length > 5 * 1024 * 1024)
                        {
                            Console.WriteLine("ERROR: El archivo es demasiado grande (máximo 5MB)");
                            continue;
                        }

                        pdfBytes = File.ReadAllBytes(filePath);
                        pdfValido = true;
                        Console.WriteLine("PDF cargado exitosamente.");
                    }
                    else
                    {
                        Console.WriteLine("ERROR: El archivo no existe. Intente nuevamente.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR al leer el archivo: {ex.Message}");
                    Console.WriteLine("Por favor, intente nuevamente.");
                }
            }

            denuncia.PdfDenuncia = pdfBytes;
        }

        // Guardar la denuncia
        try
        {
            int denunciaId = dbHelper.CrearDenuncia(denuncia);
            Console.WriteLine($"\nDenuncia registrada exitosamente con ID: {denunciaId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nError al registrar denuncia: {ex.Message}");
        }

        Console.WriteLine("Presione cualquier tecla para continuar...");
        Console.ReadKey();
    }

    static string GenerarNumeroDenuncia()
    {
        // Formato: FRA-YYYYMMDD-XXXX (donde XXXX es un número secuencial)
        string prefix = "FRA-" + DateTime.Now.ToString("yyyyMMdd") + "-";

        // En una implementación real, esto debería consultar la base de datos para obtener el último número
        Random rnd = new Random();
        return prefix + rnd.Next(1000, 9999).ToString();
    }

    static void VerListadoDenuncias()
    {
        Console.Clear();
        Console.WriteLine("=== LISTADO DE DENUNCIAS ===");
        Console.WriteLine("Opciones de búsqueda:");
        Console.WriteLine("1. Mostrar todas las denuncias");
        Console.WriteLine("2. Buscar por cédula de víctima");
        Console.WriteLine("3. Buscar por teléfono");
        Console.WriteLine("4. Buscar por beneficiario");
        Console.Write("Seleccione una opción: ");

        string opcionBusqueda = Console.ReadLine();
        List<Denuncia> denuncias = new List<Denuncia>();

        switch (opcionBusqueda)
        {
            case "1": // Mostrar todas
                denuncias = dbHelper.ObtenerDenuncias(usuarioActual.Rol == "Admin" ? null : (int?)usuarioActual.Id, usuarioActual.Rol);
                break;

            case "2": // Buscar por cédula de víctima
                Console.Write("Ingrese cédula de víctima: ");
                string cedula = Console.ReadLine();
                denuncias = dbHelper.BuscarDenunciasPorCedulaVictima(cedula, usuarioActual);
                break;

            case "3": // Buscar por teléfono
                Console.Write("Ingrese número de teléfono: ");
                string telefono = Console.ReadLine();
                denuncias = dbHelper.BuscarDenunciasPorTelefono(telefono, usuarioActual);
                break;

            case "4": // Buscar por beneficiario
                Console.Write("Ingrese nombre o cédula de beneficiario: ");
                string beneficiario = Console.ReadLine();
                denuncias = dbHelper.BuscarDenunciasPorBeneficiario(beneficiario, usuarioActual);
                break;

            default:
                Console.WriteLine("Opción no válida. Mostrando todas las denuncias...");
                denuncias = dbHelper.ObtenerDenuncias(usuarioActual.Rol == "Admin" ? null : (int?)usuarioActual.Id, usuarioActual.Rol);
                break;
        }

        // Mostrar resultados
        if (denuncias.Count == 0)
        {
            Console.WriteLine("\nNo se encontraron denuncias con los criterios especificados.");
        }
        else
        {
            Console.WriteLine("\nID  Número        Víctima                      Cédula         Beneficiario              Estado");
            Console.WriteLine("-------------------------------------------------------------------------------------------");

            foreach (var denuncia in denuncias)
            {
                Console.WriteLine($"{denuncia.Id,-4}{denuncia.NumeroDenuncia,-14}{denuncia.NombreVictima,-28}{denuncia.CedulaVictima,-15}{denuncia.NombreBeneficiario?.ToString() ?? "N/A",-26}{denuncia.Estado}");
            }
        }

        Console.Write("\nIngrese el ID de una denuncia para ver detalles (0 para volver): ");
        if (int.TryParse(Console.ReadLine(), out int denunciaId) && denunciaId > 0)
        {
            VerDetalleDenuncia(denunciaId);
        }
    }
    static void VerDetalleDenuncia(int denunciaId)
    {
        Console.Clear();

        // Obtener la denuncia específica
        var denuncias = dbHelper.ObtenerDenuncias();
        var denuncia = denuncias.Find(d => d.Id == denunciaId);

        if (denuncia == null)
        {
            Console.WriteLine("Denuncia no encontrada.");
            Console.WriteLine("Presione cualquier tecla para continuar...");
            Console.ReadKey();
            return;
        }

        Console.WriteLine($"=== DETALLE DE DENUNCIA #{denuncia.Id} ===");
        Console.WriteLine($"Número: {denuncia.NumeroDenuncia}");
        Console.WriteLine($"Fecha registro: {denuncia.FechaRegistro:dd/MM/yyyy HH:mm}");
        Console.WriteLine($"Fecha evento: {denuncia.FechaEvento:dd/MM/yyyy}");
        Console.WriteLine($"Tipo de delito: {denuncia.TipoDelito}");
        Console.WriteLine($"Dotación policial: {denuncia.DotacionPolicial ?? "N/A"}");
        Console.WriteLine($"Ubicación: {denuncia.Municipio}, {denuncia.Provincia}");

        Console.WriteLine("\nDATOS DE LA VÍCTIMA:");
        Console.WriteLine($"Nombre: {denuncia.NombreVictima}");
        Console.WriteLine($"Cédula: {denuncia.CedulaVictima}");
        Console.WriteLine($"Teléfonos: {string.Join(", ", denuncia.TelefonosVictima)}");

        if (!string.IsNullOrEmpty(denuncia.NombreBeneficiario))
        {
            Console.WriteLine("\nDATOS DEL BENEFICIARIO:");
            Console.WriteLine($"Nombre: {denuncia.NombreBeneficiario}");
            Console.WriteLine($"Cédula: {denuncia.CedulaBeneficiario ?? "N/A"}");

            if (denuncia.TelefonosBeneficiario != null && denuncia.TelefonosBeneficiario.Count > 0)
            {
                Console.WriteLine($"Teléfonos: {string.Join(", ", denuncia.TelefonosBeneficiario)}");
            }

            Console.WriteLine($"Monto del fraude: {denuncia.MontoFraude?.ToString("C") ?? "N/A"}");

            if (denuncia.CuentasBancarias != null && denuncia.CuentasBancarias.Count > 0)
            {
                Console.WriteLine("Cuentas bancarias:");
                foreach (var cuenta in denuncia.CuentasBancarias)
                {
                    Console.WriteLine($"- {cuenta.Banco ?? "Banco no especificado"}: {cuenta.NumeroCuenta}");
                }
            }
        }

        Console.WriteLine($"\nProcedencia: {denuncia.ProcedenciaDenuncia}");
        Console.WriteLine($"Estado: {denuncia.Estado}");

        if (denuncia.UsuarioAsignadoId.HasValue)
        {
            Console.WriteLine($"Asignado a: {denuncia.NombreUsuarioAsignado}");
            Console.WriteLine($"Fecha asignación: {denuncia.FechaAsignacion:dd/MM/yyyy HH:mm}");
        }

        Console.WriteLine("\nHISTORIAL DE CAMBIOS:");
        var logs = dbHelper.ObtenerLogsCambios(denunciaId);
        if (logs.Count == 0)
        {
            Console.WriteLine("No hay cambios registrados.");
        }
        else
        {
            foreach (var log in logs)
            {
                Console.WriteLine($"{log.FechaCambio:dd/MM/yyyy HH:mm} - {log.NombreUsuario}: {log.CambioRealizado}");
            }
        }

        // Mostrar opciones según el rol del usuario
        if (usuarioActual.Rol == "Admin" || (usuarioActual.Rol == "Usuario" && denuncia.UsuarioCreadorId == usuarioActual.Id))
        {
            Console.WriteLine("\n1. Editar denuncia");

            if (usuarioActual.Rol == "Admin")
            {
                Console.WriteLine("2. Asignar/Reasignar caso");
                Console.WriteLine("3. Cambiar estado del caso");
            }

            Console.WriteLine("0. Volver");
            Console.Write("Seleccione una opción: ");

            string opcion = Console.ReadLine();

            switch (opcion)
            {
                case "1":
                    EditarDenuncia(denuncia);
                    break;
                case "2" when usuarioActual.Rol == "Admin":
                    AsignarDenuncia(denuncia);
                    break;
                case "3" when usuarioActual.Rol == "Admin":
                    CambiarEstadoDenuncia(denuncia);
                    break;
            }
        }
        else
        {
            Console.WriteLine("\nPresione cualquier tecla para volver...");
            Console.ReadKey();
        }
    }

    static void EditarDenuncia(Denuncia denuncia)
    {
        Console.Clear();
        Console.WriteLine("=== EDITAR DENUNCIA ===");

        // Clonar la denuncia para no modificar la original directamente
        Denuncia copiaDenuncia = new Denuncia
        {
            Id = denuncia.Id,
            NumeroDenuncia = denuncia.NumeroDenuncia,
            FechaRegistro = denuncia.FechaRegistro,
            FechaEvento = denuncia.FechaEvento,
            TipoDelito = denuncia.TipoDelito,
            DotacionPolicial = denuncia.DotacionPolicial,
            Provincia = denuncia.Provincia,
            Municipio = denuncia.Municipio,
            NombreVictima = denuncia.NombreVictima,
            CedulaVictima = denuncia.CedulaVictima,
            TelefonosVictima = new List<string>(denuncia.TelefonosVictima),
            NombreBeneficiario = denuncia.NombreBeneficiario,
            CedulaBeneficiario = denuncia.CedulaBeneficiario,
            TelefonosBeneficiario = denuncia.TelefonosBeneficiario != null ? new List<string>(denuncia.TelefonosBeneficiario) : null,
            MontoFraude = denuncia.MontoFraude,
            CuentasBancarias = denuncia.CuentasBancarias != null ? new List<CuentaBancaria>(denuncia.CuentasBancarias) : null,
            Banco = denuncia.Banco,
            ProcedenciaDenuncia = denuncia.ProcedenciaDenuncia,
            PdfDenuncia = denuncia.PdfDenuncia,
            UsuarioAsignadoId = denuncia.UsuarioAsignadoId,
            Estado = denuncia.Estado,
            FechaAsignacion = denuncia.FechaAsignacion,
            UsuarioCreadorId = denuncia.UsuarioCreadorId
        };

        // Permitir editar campos
        Console.WriteLine("Deje en blanco los campos que no desea modificar");

        Console.Write($"Fecha del evento ({denuncia.FechaEvento:dd/MM/yyyy}): ");
        string fechaStr = Console.ReadLine();
        if (!string.IsNullOrEmpty(fechaStr))
        {
            copiaDenuncia.FechaEvento = DateTime.Parse(fechaStr);
        }

        Console.Write($"Tipo de delito ({denuncia.TipoDelito}): ");
        string tipoDelito = Console.ReadLine();
        if (!string.IsNullOrEmpty(tipoDelito))
        {
            copiaDenuncia.TipoDelito = tipoDelito;
        }

        Console.Write($"Dotación policial ({denuncia.DotacionPolicial ?? "N/A"}): ");
        string dotacion = Console.ReadLine();
        copiaDenuncia.DotacionPolicial = string.IsNullOrEmpty(dotacion) ? denuncia.DotacionPolicial : (dotacion == "N/A" ? null : dotacion);

        Console.Write($"Provincia ({denuncia.Provincia}): ");
        string provincia = Console.ReadLine();
        if (!string.IsNullOrEmpty(provincia))
        {
            copiaDenuncia.Provincia = provincia;
        }

        Console.Write($"Municipio ({denuncia.Municipio}): ");
        string municipio = Console.ReadLine();
        if (!string.IsNullOrEmpty(municipio))
        {
            copiaDenuncia.Municipio = municipio;
        }

        Console.WriteLine("\nDATOS DE LA VÍCTIMA");
        Console.Write($"Nombre ({denuncia.NombreVictima}): ");
        string nombreVictima = Console.ReadLine();
        if (!string.IsNullOrEmpty(nombreVictima))
        {
            copiaDenuncia.NombreVictima = nombreVictima;
        }

        Console.Write($"Cédula ({denuncia.CedulaVictima}): ");
        string cedulaVictima = Console.ReadLine();
        if (!string.IsNullOrEmpty(cedulaVictima))
        {
            copiaDenuncia.CedulaVictima = cedulaVictima;
        }

        Console.WriteLine("Teléfonos (actuales: " + string.Join(", ", denuncia.TelefonosVictima) + ")");
        Console.WriteLine("Ingrese nuevos teléfonos (uno por línea, vacío para mantener actuales):");
        List<string> nuevosTelefonos = new List<string>();
        string telefono;
        do
        {
            telefono = Console.ReadLine();
            if (!string.IsNullOrEmpty(telefono))
            {
                nuevosTelefonos.Add(telefono);
            }
        } while (!string.IsNullOrEmpty(telefono));

        if (nuevosTelefonos.Count > 0)
        {
            copiaDenuncia.TelefonosVictima = nuevosTelefonos;
        }

        // Datos del beneficiario
        Console.WriteLine("\nDATOS DEL BENEFICIARIO");
        Console.Write($"Nombre ({denuncia.NombreBeneficiario ?? "N/A"}): ");
        string nombreBeneficiario = Console.ReadLine();
        copiaDenuncia.NombreBeneficiario = string.IsNullOrEmpty(nombreBeneficiario) ? denuncia.NombreBeneficiario : (nombreBeneficiario == "N/A" ? null : nombreBeneficiario);

        if (!string.IsNullOrEmpty(copiaDenuncia.NombreBeneficiario))
        {
            Console.Write($"Cédula ({denuncia.CedulaBeneficiario ?? "N/A"}): ");
            string cedulaBeneficiario = Console.ReadLine();
            copiaDenuncia.CedulaBeneficiario = string.IsNullOrEmpty(cedulaBeneficiario) ? denuncia.CedulaBeneficiario : (cedulaBeneficiario == "N/A" ? null : cedulaBeneficiario);

            // Teléfonos beneficiario
            if (denuncia.TelefonosBeneficiario != null && denuncia.TelefonosBeneficiario.Count > 0)
            {
                Console.WriteLine("Teléfonos (actuales: " + string.Join(", ", denuncia.TelefonosBeneficiario) + ")");
            }
            else
            {
                Console.WriteLine("Teléfonos (actuales: N/A)");
            }

            Console.WriteLine("Ingrese nuevos teléfonos (uno por línea, vacío para mantener actuales):");
            List<string> nuevosTelBeneficiario = new List<string>();
            do
            {
                telefono = Console.ReadLine();
                if (!string.IsNullOrEmpty(telefono))
                {
                    nuevosTelBeneficiario.Add(telefono);
                }
            } while (!string.IsNullOrEmpty(telefono));

            if (nuevosTelBeneficiario.Count > 0)
            {
                copiaDenuncia.TelefonosBeneficiario = nuevosTelBeneficiario;
            }
            else if (denuncia.TelefonosBeneficiario != null)
            {
                copiaDenuncia.TelefonosBeneficiario = new List<string>(denuncia.TelefonosBeneficiario);
            }

            Console.Write($"Monto del fraude ({denuncia.MontoFraude?.ToString("C") ?? "N/A"}): ");
            string montoStr = Console.ReadLine();
            if (!string.IsNullOrEmpty(montoStr))
            {
                copiaDenuncia.MontoFraude = montoStr == "N/A" ? null : (decimal?)decimal.Parse(montoStr);
            }

            // Cuentas bancarias
            if (denuncia.CuentasBancarias != null && denuncia.CuentasBancarias.Count > 0)
            {
                Console.WriteLine("Cuentas bancarias actuales:");
                foreach (var cuenta in denuncia.CuentasBancarias)
                {
                    Console.WriteLine($"- {cuenta.Banco ?? "Banco no especificado"}: {cuenta.NumeroCuenta}");
                }
            }
            else
            {
                Console.WriteLine("Cuentas bancarias actuales: N/A");
            }

            Console.WriteLine("Ingrese nuevas cuentas en formato 'Banco,Número' (vacío para mantener actuales):");
            List<CuentaBancaria> nuevasCuentas = new List<CuentaBancaria>();
            string cuentaStr;
            do
            {
                cuentaStr = Console.ReadLine();
                if (!string.IsNullOrEmpty(cuentaStr))
                {
                    var partes = cuentaStr.Split(',');
                    if (partes.Length >= 2)
                    {
                        nuevasCuentas.Add(new CuentaBancaria
                        {
                            Banco = partes[0].Trim(),
                            NumeroCuenta = partes[1].Trim()
                        });
                    }
                }
            } while (!string.IsNullOrEmpty(cuentaStr));

            if (nuevasCuentas.Count > 0)
            {
                copiaDenuncia.CuentasBancarias = nuevasCuentas;
            }
            else if (denuncia.CuentasBancarias != null)
            {
                copiaDenuncia.CuentasBancarias = new List<CuentaBancaria>(denuncia.CuentasBancarias);
            }
        }

        // Procedencia de la denuncia
        Console.WriteLine($"\nProcedencia actual: {denuncia.ProcedenciaDenuncia}");
        Console.WriteLine("1. Policía Nacional");
        Console.WriteLine("2. Ministerio Público");
        Console.Write("Seleccione nueva procedencia (vacío para mantener actual): ");
        string procedencia = Console.ReadLine();
        if (!string.IsNullOrEmpty(procedencia))
        {
            copiaDenuncia.ProcedenciaDenuncia = procedencia == "1" ? "Policía Nacional" : "Ministerio Público";
        }

        // PDF adjunto
        Console.Write("\n¿Desea cambiar el PDF adjunto? (s/n): ");
        if (Console.ReadLine().ToLower() == "s")
        {
            Console.Write("Ruta del nuevo archivo PDF (vacío para eliminar actual): ");
            string filePath = Console.ReadLine();

            if (string.IsNullOrEmpty(filePath))
            {
                copiaDenuncia.PdfDenuncia = null;
            }
            else if (File.Exists(filePath))
            {
                copiaDenuncia.PdfDenuncia = File.ReadAllBytes(filePath);
            }
            else
            {
                Console.WriteLine("El archivo no existe. No se cambiará el PDF.");
            }
        }

        // Registrar los cambios
        string cambios = "Edición de denuncia";
        try
        {
            dbHelper.ActualizarDenuncia(copiaDenuncia, usuarioActual.Id, cambios);
            Console.WriteLine("\nDenuncia actualizada exitosamente.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nError al actualizar denuncia: {ex.Message}");
        }

        Console.WriteLine("Presione cualquier tecla para continuar...");
        Console.ReadKey();
    }

    static void AsignarDenuncia(Denuncia denuncia)
    {
        Console.Clear();
        Console.WriteLine("=== ASIGNAR DENUNCIA ===");

        // Obtener lista de usuarios disponibles
        var usuarios = dbHelper.ObtenerUsuarios();

        Console.WriteLine("Usuarios disponibles:");
        foreach (var usuario in usuarios)
        {
            Console.WriteLine($"{usuario.Id}. {usuario.Nombre} ({usuario.Rol})");
        }

        Console.Write("\nIngrese el ID del usuario a asignar (0 para desasignar): ");
        if (int.TryParse(Console.ReadLine(), out int usuarioId))
        {
            if (usuarioId == 0)
            {
                denuncia.UsuarioAsignadoId = null;
                denuncia.Estado = "No asignado";
                denuncia.FechaAsignacion = null;
            }
            else if (usuarios.Exists(u => u.Id == usuarioId))
            {
                denuncia.UsuarioAsignadoId = usuarioId;
                denuncia.Estado = "Asignado";
                denuncia.FechaAsignacion = DateTime.Now;
            }
            else
            {
                Console.WriteLine("ID de usuario no válido.");
                Console.WriteLine("Presione cualquier tecla para continuar...");
                Console.ReadKey();
                return;
            }

            string cambios = $"Asignación modificada. Nuevo estado: {denuncia.Estado}";
            if (denuncia.UsuarioAsignadoId.HasValue)
            {
                var usuarioAsignado = usuarios.Find(u => u.Id == denuncia.UsuarioAsignadoId.Value);
                cambios += $". Asignado a: {usuarioAsignado.Nombre}";
            }
            else
            {
                cambios += ". Sin asignar";
            }

            try
            {
                dbHelper.ActualizarDenuncia(denuncia, usuarioActual.Id, cambios);
                Console.WriteLine("\nDenuncia actualizada exitosamente.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError al actualizar denuncia: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine("Entrada no válida.");
        }

        Console.WriteLine("Presione cualquier tecla para continuar...");
        Console.ReadKey();
    }

    static void CambiarEstadoDenuncia(Denuncia denuncia)
    {
        Console.Clear();
        Console.WriteLine("=== CAMBIAR ESTADO DE DENUNCIA ===");
        Console.WriteLine($"Estado actual: {denuncia.Estado}");

        Console.WriteLine("\nEstados disponibles:");
        Console.WriteLine("1. Asignado");
        Console.WriteLine("2. No asignado");
        Console.WriteLine("3. En proceso");
        Console.WriteLine("4. Cerrado");

        Console.Write("Seleccione el nuevo estado: ");
        string opcion = Console.ReadLine();

        string nuevoEstado = denuncia.Estado;
        switch (opcion)
        {
            case "1":
                nuevoEstado = "Asignado";
                break;
            case "2":
                nuevoEstado = "No asignado";
                break;
            case "3":
                nuevoEstado = "En proceso";
                break;
            case "4":
                nuevoEstado = "Cerrado";
                break;
            default:
                Console.WriteLine("Opción no válida.");
                Console.WriteLine("Presione cualquier tecla para continuar...");
                Console.ReadKey();
                return;
        }

        denuncia.Estado = nuevoEstado;
        string cambios = $"Estado cambiado de {denuncia.Estado} a {nuevoEstado}";

        try
        {
            dbHelper.ActualizarDenuncia(denuncia, usuarioActual.Id, cambios);
            Console.WriteLine("\nEstado actualizado exitosamente.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nError al actualizar estado: {ex.Message}");
        }

        Console.WriteLine("Presione cualquier tecla para continuar...");
        Console.ReadKey();
    }

    static void BuscarDenuncias()
    {
        Console.Clear();
        Console.WriteLine("=== BUSCAR DENUNCIAS ===");
        Console.WriteLine("1. Buscar por número de denuncia");
        Console.WriteLine("2. Buscar por cédula de víctima");
        Console.WriteLine("3. Buscar por nombre de víctima");
        Console.WriteLine("4. Buscar por cédula de beneficiario");
        Console.WriteLine("5. Buscar por nombre de beneficiario");
        Console.WriteLine("6. Buscar por teléfono");
        Console.WriteLine("7. Buscar por número de cuenta bancaria");
        Console.WriteLine("0. Volver");

        Console.Write("Seleccione una opción: ");
        string opcion = Console.ReadLine();

        if (opcion == "0") return;

        Console.Write("Ingrese el término de búsqueda: ");
        string termino = Console.ReadLine();

        if (string.IsNullOrEmpty(termino))
        {
            Console.WriteLine("Término de búsqueda no puede estar vacío.");
            Console.WriteLine("Presione cualquier tecla para continuar...");
            Console.ReadKey();
            return;
        }

        string campo = "";
        string consulta = "SELECT d.Id, d.NumeroDenuncia, d.NombreVictima, d.CedulaVictima, d.NombreBeneficiario, d.Estado FROM Denuncias d ";

        switch (opcion)
        {
            case "1":
                campo = "Número de denuncia";
                consulta += "WHERE d.NumeroDenuncia LIKE @Termino";
                break;
            case "2":
                campo = "Cédula de víctima";
                consulta += "WHERE d.CedulaVictima LIKE @Termino";
                break;
            case "3":
                campo = "Nombre de víctima";
                consulta += "WHERE d.NombreVictima LIKE @Termino";
                break;
            case "4":
                campo = "Cédula de beneficiario";
                consulta += "WHERE d.CedulaBeneficiario LIKE @Termino";
                break;
            case "5":
                campo = "Nombre de beneficiario";
                consulta += "WHERE d.NombreBeneficiario LIKE @Termino";
                break;
            case "6":
                campo = "Teléfono";
                consulta += "JOIN (SELECT DenunciaId FROM TelefonosVictima WHERE Telefono LIKE @Termino " +
                           "UNION SELECT DenunciaId FROM TelefonosBeneficiario WHERE Telefono LIKE @Termino) t ON d.Id = t.DenunciaId";
                break;
            case "7":
                campo = "Número de cuenta bancaria";
                consulta += "JOIN CuentasBancarias c ON d.Id = c.DenunciaId WHERE c.NumeroCuenta LIKE @Termino";
                break;
            default:
                Console.WriteLine("Opción no válida.");
                Console.WriteLine("Presione cualquier tecla para continuar...");
                Console.ReadKey();
                return;
        }

        // Aplicar filtro de usuario si no es admin
        if (usuarioActual.Rol != "Admin")
        {
            if (consulta.Contains("WHERE"))
            {
                consulta += " AND d.UsuarioCreadorId = @UsuarioId";
            }
            else
            {
                consulta += " WHERE d.UsuarioCreadorId = @UsuarioId";
            }
        }

        var parametros = new Dictionary<string, object>
        {
            { "@Termino", $"%{termino}%" }
        };

        if (usuarioActual.Rol != "Admin")
        {
            parametros.Add("@UsuarioId", usuarioActual.Id);
        }

        DataTable resultados = dbHelper.GenerarReporte(consulta, parametros);

        Console.Clear();
        Console.WriteLine($"=== RESULTADOS DE BÚSQUEDA ({campo}: {termino}) ===");

        if (resultados.Rows.Count == 0)
        {
            Console.WriteLine("No se encontraron denuncias que coincidan con la búsqueda.");
        }
        else
        {
            Console.WriteLine("ID  Número        Víctima                      Cédula         Beneficiario              Estado");
            Console.WriteLine("-------------------------------------------------------------------------------------------");

            foreach (DataRow row in resultados.Rows)
            {
                Console.WriteLine($"{row["Id"],-4}{row["NumeroDenuncia"],-14}{row["NombreVictima"],-28}{row["CedulaVictima"],-15}{row["NombreBeneficiario"]?.ToString() ?? "N/A",-26}{row["Estado"]}");
            }

            Console.Write("\nIngrese el ID de una denuncia para ver detalles (0 para volver): ");
            if (int.TryParse(Console.ReadLine(), out int denunciaId) && denunciaId > 0)
            {
                VerDetalleDenuncia(denunciaId);
            }
        }

        Console.WriteLine("\nPresione cualquier tecla para continuar...");
        Console.ReadKey();
    }

    static void GenerarReportes()
    {
        Console.Clear();
        Console.WriteLine("=== GENERAR REPORTES ESTADÍSTICOS ===");
        Console.WriteLine("1. Casos por fecha de ingreso");
        Console.WriteLine("2. Casos por fecha del fraude");
        Console.WriteLine("3. Casos por provincia/municipio");
        Console.WriteLine("4. Casos por tipo de delito");
        Console.WriteLine("5. Casos por estado");
        Console.WriteLine("6. Casos por usuario asignado");
        Console.WriteLine("7. Casos resueltos vs. pendientes");
        Console.WriteLine("8. Casos por beneficiario o víctima");
        Console.WriteLine("0. Volver");

        Console.Write("Seleccione una opción: ");
        string opcion = Console.ReadLine();

        if (opcion == "0") return;

        string consulta = "";
        string titulo = "";
        Dictionary<string, object> parametros = new Dictionary<string, object>();

        switch (opcion)
        {
            case "1":
                titulo = "CASOS POR FECHA DE INGRESO";
                Console.Write("Ingrese año (vacío para todos): ");
                string anio = Console.ReadLine();

                if (!string.IsNullOrEmpty(anio))
                {
                    consulta = @"SELECT CONVERT(VARCHAR(10), FechaRegistro, 103) AS Fecha, 
                           COUNT(*) AS Cantidad 
                           FROM Denuncias 
                           WHERE YEAR(FechaRegistro) = @Anio
                           GROUP BY CONVERT(VARCHAR(10), FechaRegistro, 103)
                           ORDER BY MIN(FechaRegistro)"; // Cambiado a MIN(FechaRegistro)
                    parametros.Add("@Anio", anio);
                }
                else
                {
                    consulta = @"SELECT CONVERT(VARCHAR(10), FechaRegistro, 103) AS Fecha, 
                           COUNT(*) AS Cantidad 
                           FROM Denuncias 
                           GROUP BY CONVERT(VARCHAR(10), FechaRegistro, 103)
                           ORDER BY MIN(FechaRegistro)"; // Cambiado a MIN(FechaRegistro)
                }
                break;

            case "2":
                titulo = "CASOS POR FECHA DEL FRAUDE";
                Console.Write("Ingrese año (vacío para todos): ");
                anio = Console.ReadLine();

                if (!string.IsNullOrEmpty(anio))
                {
                    consulta = @"SELECT CONVERT(VARCHAR(10), FechaEvento, 103) AS Fecha, 
                           COUNT(*) AS Cantidad 
                           FROM Denuncias 
                           WHERE YEAR(FechaEvento) = @Anio
                           GROUP BY CONVERT(VARCHAR(10), FechaEvento, 103)
                           ORDER BY MIN(FechaEvento)"; // Cambiado a MIN(FechaEvento)
                    parametros.Add("@Anio", anio);
                }
                else
                {
                    consulta = @"SELECT CONVERT(VARCHAR(10), FechaEvento, 103) AS Fecha, 
                           COUNT(*) AS Cantidad 
                           FROM Denuncias 
                           GROUP BY CONVERT(VARCHAR(10), FechaEvento, 103)
                           ORDER BY MIN(FechaEvento)"; // Cambiado a MIN(FechaEvento)
                }
                break;

            case "3":
                titulo = "CASOS POR PROVINCIA/MUNICIPIO";
                consulta = @"SELECT Provincia, Municipio, COUNT(*) AS Cantidad 
                       FROM Denuncias 
                       GROUP BY Provincia, Municipio
                       ORDER BY Provincia, Municipio";
                break;

            case "4":
                titulo = "CASOS POR TIPO DE DELITO";
                consulta = @"SELECT TipoDelito, COUNT(*) AS Cantidad 
                       FROM Denuncias 
                       GROUP BY TipoDelito
                       ORDER BY Cantidad DESC";
                break;

            case "5":
                titulo = "CASOS POR ESTADO";
                consulta = @"SELECT Estado, COUNT(*) AS Cantidad 
                       FROM Denuncias 
                       GROUP BY Estado
                       ORDER BY Cantidad DESC";
                break;

            case "6":
                titulo = "CASOS POR USUARIO ASIGNADO";
                consulta = @"SELECT u.Nombre AS Usuario, COUNT(*) AS Cantidad 
                       FROM Denuncias d
                       LEFT JOIN Usuarios u ON d.UsuarioAsignadoId = u.Id
                       GROUP BY u.Nombre
                       ORDER BY Cantidad DESC";
                break;

            case "7":
                titulo = "CASOS RESUELTOS VS. PENDIENTES";
                consulta = @"SELECT 
                       SUM(CASE WHEN Estado = 'Cerrado' THEN 1 ELSE 0 END) AS Resueltos,
                       SUM(CASE WHEN Estado != 'Cerrado' THEN 1 ELSE 0 END) AS Pendientes
                       FROM Denuncias";
                break;

            case "8":
                titulo = "CASOS POR BENEFICIARIO O VÍCTIMA";
                Console.Write("¿Buscar por víctima (1) o beneficiario (2)? ");
                string tipo = Console.ReadLine();

                if (tipo == "1")
                {
                    consulta = @"SELECT NombreVictima AS Nombre, CedulaVictima AS Cedula, COUNT(*) AS Cantidad 
                           FROM Denuncias 
                           GROUP BY NombreVictima, CedulaVictima
                           ORDER BY Cantidad DESC";
                }
                else
                {
                    consulta = @"SELECT NombreBeneficiario AS Nombre, CedulaBeneficiario AS Cedula, COUNT(*) AS Cantidad 
                           FROM Denuncias 
                           WHERE NombreBeneficiario IS NOT NULL
                           GROUP BY NombreBeneficiario, CedulaBeneficiario
                           ORDER BY Cantidad DESC";
                }
                break;

            default:
                Console.WriteLine("Opción no válida.");
                Console.WriteLine("Presione cualquier tecla para continuar...");
                Console.ReadKey();
                return;
        }
        DataTable reporte = dbHelper.GenerarReporte(consulta, parametros);

        Console.Clear();
        Console.WriteLine($"=== {titulo} ===");

        // Mostrar datos en consola (código existente)

        Console.WriteLine("\n1. Exportar a PDF");
        Console.WriteLine("2. Exportar a CSV");
        Console.WriteLine("0. Volver");
        Console.Write("Seleccione una opción: ");

        string exportar = Console.ReadLine();
        if (exportar == "1" || exportar == "2")
        {
            Console.Write("Ingrese la ruta donde guardar el archivo (ej: C:\\reportes\\reporte.pdf): ");
            string filePath = Console.ReadLine();

            try
            {
                if (exportar == "1")
                {
                    dbHelper.ExportarAPdf(reporte, titulo, filePath);
                    Console.WriteLine($"Reporte PDF generado exitosamente en: {filePath}");
                }
                else
                {
                    dbHelper.ExportarACsv(reporte, filePath);
                    Console.WriteLine($"Reporte CSV generado exitosamente en: {filePath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al exportar: {ex.Message}");
            }

            Console.WriteLine("Presione cualquier tecla para continuar...");
            Console.ReadKey();


            // En el método de exportación
            if (string.IsNullOrWhiteSpace(filePath))
            {
                Console.WriteLine("La ruta no puede estar vacía.");
                return;
            }

            string extension = Path.GetExtension(filePath).ToLower();
            if (exportar == "1" && extension != ".pdf")
            {
                filePath += ".pdf";
            }
            else if (exportar == "2" && extension != ".csv")
            {
                filePath += ".csv";
            }

            // Verificar si el directorio existe
            string directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
            {
                Console.WriteLine($"El directorio {directory} no existe. ¿Desea crearlo? (s/n)");
                if (Console.ReadLine().ToLower() == "s")
                {
                    Directory.CreateDirectory(directory);
                }
                else
                {
                    return;
                }
            }
        }

    }


        static void AdministrarUsuarios()
    {
        if (usuarioActual.Rol != "Admin")
        {
            Console.WriteLine("Acceso denegado.");
            Console.WriteLine("Presione cualquier tecla para continuar...");
            Console.ReadKey();
            return;
        }

        while (true)
        {
            Console.Clear();
            Console.WriteLine("=== ADMINISTRAR USUARIOS ===");

            var usuarios = dbHelper.ObtenerUsuarios();

            Console.WriteLine("ID  Nombre                     Email                        Rol");
            Console.WriteLine("---------------------------------------------------------------");
            foreach (var usuario in usuarios)
            {
                Console.WriteLine($"{usuario.Id,-4}{usuario.Nombre,-28}{usuario.Email,-28}{usuario.Rol}");
            }

            Console.WriteLine("\n1. Agregar nuevo usuario");
            Console.WriteLine("2. Editar usuario");
            Console.WriteLine("3. Eliminar usuario");
            Console.WriteLine("0. Volver");
            Console.Write("Seleccione una opción: ");

            string opcion = Console.ReadLine();

            switch (opcion)
            {
                case "1":
                    AgregarUsuario();
                    break;
                case "2":
                    EditarUsuario(usuarios);
                    break;
                case "3":
                    EliminarUsuario(usuarios);
                    break;
                case "0":
                    return;
                default:
                    Console.WriteLine("Opción no válida.");
                    Console.WriteLine("Presione cualquier tecla para continuar...");
                    Console.ReadKey();
                    break;
            }
        }
    }

    static void AgregarUsuario()
    {
        Console.Clear();
        Console.WriteLine("=== AGREGAR NUEVO USUARIO ===");

        Console.Write("Nombre completo: ");
        string nombre = Console.ReadLine();

        Console.Write("Email: ");
        string email = Console.ReadLine();

        Console.Write("Contraseña: ");
        string password = Console.ReadLine();

        Console.WriteLine("Rol:");
        Console.WriteLine("1. Administrador");
        Console.WriteLine("2. Usuario normal");
        Console.Write("Seleccione: ");
        string rol = Console.ReadLine() == "1" ? "Admin" : "Usuario";

        try
        {
            using (SqlConnection connection = new SqlConnection(dbHelper.ConnectionString))
            {
                connection.Open();

                string query = "INSERT INTO Usuarios (Nombre, Email, Password, Rol) VALUES (@Nombre, @Email, @Password, @Rol)";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Nombre", nombre);
                    command.Parameters.AddWithValue("@Email", email);
                    command.Parameters.AddWithValue("@Password", password);
                    command.Parameters.AddWithValue("@Rol", rol);

                    int affectedRows = command.ExecuteNonQuery();

                    if (affectedRows > 0)
                    {
                        Console.WriteLine("\nUsuario agregado exitosamente.");
                    }
                    else
                    {
                        Console.WriteLine("\nNo se pudo agregar el usuario.");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nError al agregar usuario: {ex.Message}");
        }

        Console.WriteLine("Presione cualquier tecla para continuar...");
        Console.ReadKey();
    }

    static void EditarUsuario(List<Usuario> usuarios)
    {
        Console.Write("\nIngrese el ID del usuario a editar: ");
        if (int.TryParse(Console.ReadLine(), out int usuarioId))
        {
            var usuario = usuarios.Find(u => u.Id == usuarioId);

            if (usuario == null)
            {
                Console.WriteLine("Usuario no encontrado.");
                Console.WriteLine("Presione cualquier tecla para continuar...");
                Console.ReadKey();
                return;
            }

            Console.Clear();
            Console.WriteLine($"=== EDITAR USUARIO #{usuario.Id} ===");

            Console.Write($"Nombre ({usuario.Nombre}): ");
            string nombre = Console.ReadLine();

            Console.Write($"Email ({usuario.Email}): ");
            string email = Console.ReadLine();

            Console.Write("Nueva contraseña (dejar vacío para no cambiar): ");
            string password = Console.ReadLine();

            Console.WriteLine($"Rol actual: {usuario.Rol}");
            Console.WriteLine("1. Administrador");
            Console.WriteLine("2. Usuario normal");
            Console.Write("Seleccione nuevo rol (vacío para no cambiar): ");
            string rolOpcion = Console.ReadLine();
            string rol = string.IsNullOrEmpty(rolOpcion) ? usuario.Rol : (rolOpcion == "1" ? "Admin" : "Usuario");

            try
            {
                using (SqlConnection connection = new SqlConnection(dbHelper.ConnectionString))
                {
                    connection.Open();

                    string query = @"UPDATE Usuarios SET 
                                   Nombre = @Nombre, 
                                   Email = @Email, 
                                   " + (!string.IsNullOrEmpty(password) ? "Password = @Password, " : "") + @"
                                   Rol = @Rol
                                   WHERE Id = @Id";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Nombre", string.IsNullOrEmpty(nombre) ? usuario.Nombre : nombre);
                        command.Parameters.AddWithValue("@Email", string.IsNullOrEmpty(email) ? usuario.Email : email);
                        if (!string.IsNullOrEmpty(password))
                        {
                            command.Parameters.AddWithValue("@Password", password);
                        }
                        command.Parameters.AddWithValue("@Rol", rol);
                        command.Parameters.AddWithValue("@Id", usuario.Id);

                        int affectedRows = command.ExecuteNonQuery();

                        if (affectedRows > 0)
                        {
                            Console.WriteLine("\nUsuario actualizado exitosamente.");
                        }
                        else
                        {
                            Console.WriteLine("\nNo se pudo actualizar el usuario.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError al actualizar usuario: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine("ID de usuario no válido.");
        }

        Console.WriteLine("Presione cualquier tecla para continuar...");
        Console.ReadKey();
    }

    static void EliminarUsuario(List<Usuario> usuarios)
    {
        Console.Write("\nIngrese el ID del usuario a eliminar: ");
        if (int.TryParse(Console.ReadLine(), out int usuarioId))
        {
            if (usuarioId == usuarioActual.Id)
            {
                Console.WriteLine("No puede eliminarse a sí mismo.");
                Console.WriteLine("Presione cualquier tecla para continuar...");
                Console.ReadKey();
                return;
            }

            var usuario = usuarios.Find(u => u.Id == usuarioId);

            if (usuario == null)
            {
                Console.WriteLine("Usuario no encontrado.");
                Console.WriteLine("Presione cualquier tecla para continuar...");
                Console.ReadKey();
                return;
            }

            Console.WriteLine($"¿Está seguro que desea eliminar al usuario {usuario.Nombre} ({usuario.Email})? (s/n)");
            string confirmacion = Console.ReadLine();

            if (confirmacion.ToLower() == "s")
            {
                try
                {
                    using (SqlConnection connection = new SqlConnection(dbHelper.ConnectionString))
                    {
                        connection.Open();

                        // Primero, desasignar cualquier denuncia asignada a este usuario
                        string query = "UPDATE Denuncias SET UsuarioAsignadoId = NULL WHERE UsuarioAsignadoId = @Id";

                        using (SqlCommand command = new SqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@Id", usuario.Id);
                            command.ExecuteNonQuery();
                        }

                        // Luego, eliminar el usuario
                        query = "DELETE FROM Usuarios WHERE Id = @Id";

                        using (SqlCommand command = new SqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@Id", usuario.Id);

                            int affectedRows = command.ExecuteNonQuery();

                            if (affectedRows > 0)
                            {
                                Console.WriteLine("\nUsuario eliminado exitosamente.");
                            }
                            else
                            {
                                Console.WriteLine("\nNo se pudo eliminar el usuario.");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\nError al eliminar usuario: {ex.Message}");
                }
            }
        }
        else
        {
            Console.WriteLine("ID de usuario no válido.");
        }

        Console.WriteLine("Presione cualquier tecla para continuar...");
        Console.ReadKey();
    }
}