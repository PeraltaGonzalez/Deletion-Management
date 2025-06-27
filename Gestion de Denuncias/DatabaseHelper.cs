using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using CsvHelper;
using System.Globalization;
using iTextSharp.text;
using iTextSharp.text.pdf;

public class DatabaseHelper
{
    private string connectionString;

    public string ConnectionString => connectionString;

    public DatabaseHelper(string connectionString)
    {
        this.connectionString = connectionString;
    }

    public Usuario Login(string email, string password)
    {
        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            connection.Open();
            string query = "SELECT Id, Nombre, Email, Rol FROM Usuarios WHERE Email = @Email AND Password = @Password";

            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Email", email);
                command.Parameters.AddWithValue("@Password", password);

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new Usuario
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            Nombre = reader["Nombre"].ToString(),
                            Email = reader["Email"].ToString(),
                            Rol = reader["Rol"].ToString()
                        };
                    }
                }
            }
        }
        return null;
    }

    public int CrearDenuncia(Denuncia denuncia)
    {
        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            connection.Open();
            SqlTransaction transaction = connection.BeginTransaction();

            try
            {
                // Insertar denuncia principal
                string query = @"INSERT INTO Denuncias (
                    NumeroDenuncia, FechaRegistro, FechaEvento, TipoDelito, DotacionPolicial, 
                    Provincia, Municipio, NombreVictima, CedulaVictima, NombreBeneficiario, 
                    CedulaBeneficiario, MontoFraude, Banco, ProcedenciaDenuncia, PdfDenuncia, 
                    UsuarioAsignadoId, Estado, FechaAsignacion, UsuarioCreadorId
                ) VALUES (
                    @NumeroDenuncia, @FechaRegistro, @FechaEvento, @TipoDelito, @DotacionPolicial, 
                    @Provincia, @Municipio, @NombreVictima, @CedulaVictima, @NombreBeneficiario, 
                    @CedulaBeneficiario, @MontoFraude, @Banco, @ProcedenciaDenuncia, @PdfDenuncia, 
                    @UsuarioAsignadoId, @Estado, @FechaAsignacion, @UsuarioCreadorId
                ); SELECT SCOPE_IDENTITY();";

                using (SqlCommand command = new SqlCommand(query, connection, transaction))
                {
                    command.Parameters.AddWithValue("@NumeroDenuncia", denuncia.NumeroDenuncia);
                    command.Parameters.AddWithValue("@FechaRegistro", denuncia.FechaRegistro);
                    command.Parameters.AddWithValue("@FechaEvento", denuncia.FechaEvento);
                    command.Parameters.AddWithValue("@TipoDelito", denuncia.TipoDelito);
                    command.Parameters.AddWithValue("@DotacionPolicial", (object)denuncia.DotacionPolicial ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Provincia", denuncia.Provincia);
                    command.Parameters.AddWithValue("@Municipio", denuncia.Municipio);
                    command.Parameters.AddWithValue("@NombreVictima", denuncia.NombreVictima);
                    command.Parameters.AddWithValue("@CedulaVictima", denuncia.CedulaVictima);
                    command.Parameters.AddWithValue("@NombreBeneficiario", (object)denuncia.NombreBeneficiario ?? DBNull.Value);
                    command.Parameters.AddWithValue("@CedulaBeneficiario", (object)denuncia.CedulaBeneficiario ?? DBNull.Value);
                    command.Parameters.AddWithValue("@MontoFraude", (object)denuncia.MontoFraude ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Banco", (object)denuncia.Banco ?? DBNull.Value);
                    command.Parameters.AddWithValue("@ProcedenciaDenuncia", denuncia.ProcedenciaDenuncia);
                    command.Parameters.AddWithValue("@PdfDenuncia", (object)denuncia.PdfDenuncia ?? DBNull.Value);
                    command.Parameters.AddWithValue("@UsuarioAsignadoId", (object)denuncia.UsuarioAsignadoId ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Estado", denuncia.Estado);
                    command.Parameters.AddWithValue("@FechaAsignacion", denuncia.FechaAsignacion ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@UsuarioCreadorId", denuncia.UsuarioCreadorId);

                    int denunciaId = Convert.ToInt32(command.ExecuteScalar());

                    // Insertar teléfonos de víctima
                    foreach (var telefono in denuncia.TelefonosVictima)
                    {
                        query = "INSERT INTO TelefonosVictima (DenunciaId, Telefono) VALUES (@DenunciaId, @Telefono)";
                        command.CommandText = query;
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@DenunciaId", denunciaId);
                        command.Parameters.AddWithValue("@Telefono", telefono);
                        command.ExecuteNonQuery();
                    }

                    // Insertar teléfonos de beneficiario si existe
                    if (denuncia.TelefonosBeneficiario != null)
                    {
                        foreach (var telefono in denuncia.TelefonosBeneficiario)
                        {
                            query = "INSERT INTO TelefonosBeneficiario (DenunciaId, Telefono) VALUES (@DenunciaId, @Telefono)";
                            command.CommandText = query;
                            command.Parameters.Clear();
                            command.Parameters.AddWithValue("@DenunciaId", denunciaId);
                            command.Parameters.AddWithValue("@Telefono", telefono);
                            command.ExecuteNonQuery();
                        }
                    }

                    // Insertar cuentas bancarias si existen
                    if (denuncia.CuentasBancarias != null)
                    {
                        foreach (var cuenta in denuncia.CuentasBancarias)
                        {
                            query = "INSERT INTO CuentasBancarias (DenunciaId, NumeroCuenta, Banco) VALUES (@DenunciaId, @NumeroCuenta, @Banco)";
                            command.CommandText = query;
                            command.Parameters.Clear();
                            command.Parameters.AddWithValue("@DenunciaId", denunciaId);
                            command.Parameters.AddWithValue("@NumeroCuenta", cuenta.NumeroCuenta);
                            command.Parameters.AddWithValue("@Banco", (object)cuenta.Banco ?? DBNull.Value);
                            command.ExecuteNonQuery();
                        }
                    }

                    transaction.Commit();
                    return denunciaId;
                }
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
   
public void ExportarAPdf(DataTable datos, string titulo, string filePath)
{
    Document document = new Document(PageSize.A4.Rotate());
    PdfWriter.GetInstance(document, new FileStream(filePath, FileMode.Create));
    document.Open();

    // Agregar título
    document.Add(new Paragraph(titulo, new Font(Font.FontFamily.HELVETICA, 18, Font.BOLD)));
    document.Add(new Paragraph("\n"));

    // Crear tabla
    PdfPTable table = new PdfPTable(datos.Columns.Count);
    table.WidthPercentage = 100;

    // Encabezados
    foreach (DataColumn column in datos.Columns)
    {
        PdfPCell cell = new PdfPCell(new Phrase(column.ColumnName,
            new Font(Font.FontFamily.HELVETICA, 12, Font.BOLD)));
        cell.BackgroundColor = new BaseColor(200, 200, 200);
        table.AddCell(cell);
    }

    // Datos
    foreach (DataRow row in datos.Rows)
    {
        foreach (object cell in row.ItemArray)
        {
            table.AddCell(new Phrase(cell.ToString(),
                new Font(Font.FontFamily.HELVETICA, 10)));
        }
    }

    document.Add(table);
    document.Close();
}





public void ExportarACsv(DataTable datos, string filePath)
{
    using (var writer = new StreamWriter(filePath))
    using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
    {
        // Escribir encabezados
        foreach (DataColumn column in datos.Columns)
        {
            csv.WriteField(column.ColumnName);
        }
        csv.NextRecord();

        // Escribir datos
        foreach (DataRow row in datos.Rows)
        {
            foreach (object item in row.ItemArray)
            {
                csv.WriteField(item.ToString());
            }
            csv.NextRecord();
        }
    }
}



public List<Denuncia> ObtenerDenuncias(int? usuarioId = null, string rol = null)
    {
        List<Denuncia> denuncias = new List<Denuncia>();

        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            connection.Open();

            string query = @"SELECT d.*, u.Nombre AS NombreUsuarioAsignado 
                           FROM Denuncias d
                           LEFT JOIN Usuarios u ON d.UsuarioAsignadoId = u.Id";

            if (rol == "Usuario" && usuarioId.HasValue)
            {
                query += " WHERE d.UsuarioCreadorId = @UsuarioId";
            }

            using (SqlCommand command = new SqlCommand(query, connection))
            {
                if (rol == "Usuario" && usuarioId.HasValue)
                {
                    command.Parameters.AddWithValue("@UsuarioId", usuarioId.Value);
                }

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Denuncia denuncia = new Denuncia
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            NumeroDenuncia = reader["NumeroDenuncia"].ToString(),
                            FechaRegistro = Convert.ToDateTime(reader["FechaRegistro"]),
                            FechaEvento = Convert.ToDateTime(reader["FechaEvento"]),
                            TipoDelito = reader["TipoDelito"].ToString(),
                            DotacionPolicial = reader["DotacionPolicial"]?.ToString(),
                            Provincia = reader["Provincia"].ToString(),
                            Municipio = reader["Municipio"].ToString(),
                            NombreVictima = reader["NombreVictima"].ToString(),
                            CedulaVictima = reader["CedulaVictima"].ToString(),
                            NombreBeneficiario = reader["NombreBeneficiario"]?.ToString(),
                            CedulaBeneficiario = reader["CedulaBeneficiario"]?.ToString(),
                            MontoFraude = reader["MontoFraude"] != DBNull.Value ? Convert.ToDecimal(reader["MontoFraude"]) : (decimal?)null,
                            Banco = reader["Banco"]?.ToString(),
                            ProcedenciaDenuncia = reader["ProcedenciaDenuncia"].ToString(),
                            PdfDenuncia = reader["PdfDenuncia"] as byte[],
                            UsuarioAsignadoId = reader["UsuarioAsignadoId"] != DBNull.Value ? Convert.ToInt32(reader["UsuarioAsignadoId"]) : (int?)null,
                            Estado = reader["Estado"].ToString(),
                            FechaAsignacion = reader["FechaAsignacion"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["FechaAsignacion"]) : null,                            UsuarioCreadorId = Convert.ToInt32(reader["UsuarioCreadorId"]),
                            NombreUsuarioAsignado = reader["NombreUsuarioAsignado"]?.ToString()
                        };

                        denuncias.Add(denuncia);
                    }
                }
            }

            // Obtener teléfonos y cuentas para cada denuncia
            foreach (var denuncia in denuncias)
            {
                denuncia.TelefonosVictima = ObtenerTelefonosVictima(denuncia.Id, connection);
                denuncia.TelefonosBeneficiario = ObtenerTelefonosBeneficiario(denuncia.Id, connection);
                denuncia.CuentasBancarias = ObtenerCuentasBancarias(denuncia.Id, connection);
            }
        }

        return denuncias;
    }

    private List<string> ObtenerTelefonosVictima(int denunciaId, SqlConnection connection)
    {
        List<string> telefonos = new List<string>();

        string query = "SELECT Telefono FROM TelefonosVictima WHERE DenunciaId = @DenunciaId";

        using (SqlCommand command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@DenunciaId", denunciaId);

            using (SqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    telefonos.Add(reader["Telefono"].ToString());
                }
            }
        }

        return telefonos;
    }

    private List<string> ObtenerTelefonosBeneficiario(int denunciaId, SqlConnection connection)
    {
        List<string> telefonos = new List<string>();

        string query = "SELECT Telefono FROM TelefonosBeneficiario WHERE DenunciaId = @DenunciaId";

        using (SqlCommand command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@DenunciaId", denunciaId);

            using (SqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    telefonos.Add(reader["Telefono"].ToString());
                }
            }
        }

        return telefonos;
    }






    private List<CuentaBancaria> ObtenerCuentasBancarias(int denunciaId, SqlConnection connection)
    {
        List<CuentaBancaria> cuentas = new List<CuentaBancaria>();

        string query = "SELECT NumeroCuenta, Banco FROM CuentasBancarias WHERE DenunciaId = @DenunciaId";

        using (SqlCommand command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@DenunciaId", denunciaId);

            using (SqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    cuentas.Add(new CuentaBancaria
                    {
                        NumeroCuenta = reader["NumeroCuenta"].ToString(),
                        Banco = reader["Banco"]?.ToString()
                    });
                }
            }
        }

        return cuentas;
    }

    public void ActualizarDenuncia(Denuncia denuncia, int usuarioId, string cambios)
    {
        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            connection.Open();
            SqlTransaction transaction = connection.BeginTransaction();

            try
            {
                // Actualizar denuncia principal
                string query = @"UPDATE Denuncias SET
                    NumeroDenuncia = @NumeroDenuncia,
                    FechaEvento = @FechaEvento,
                    TipoDelito = @TipoDelito,
                    DotacionPolicial = @DotacionPolicial,
                    Provincia = @Provincia,
                    Municipio = @Municipio,
                    NombreVictima = @NombreVictima,
                    CedulaVictima = @CedulaVictima,
                    NombreBeneficiario = @NombreBeneficiario,
                    CedulaBeneficiario = @CedulaBeneficiario,
                    MontoFraude = @MontoFraude,
                    Banco = @Banco,
                    ProcedenciaDenuncia = @ProcedenciaDenuncia,
                    PdfDenuncia = @PdfDenuncia,
                    UsuarioAsignadoId = @UsuarioAsignadoId,
                    Estado = @Estado,
                    FechaAsignacion = @FechaAsignacion
                WHERE Id = @Id";

                using (SqlCommand command = new SqlCommand(query, connection, transaction))
                {
                    command.Parameters.AddWithValue("@Id", denuncia.Id);
                    command.Parameters.AddWithValue("@NumeroDenuncia", denuncia.NumeroDenuncia);
                    command.Parameters.AddWithValue("@FechaEvento", denuncia.FechaEvento);
                    command.Parameters.AddWithValue("@TipoDelito", denuncia.TipoDelito);
                    command.Parameters.AddWithValue("@DotacionPolicial", (object)denuncia.DotacionPolicial ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Provincia", denuncia.Provincia);
                    command.Parameters.AddWithValue("@Municipio", denuncia.Municipio);
                    command.Parameters.AddWithValue("@NombreVictima", denuncia.NombreVictima);
                    command.Parameters.AddWithValue("@CedulaVictima", denuncia.CedulaVictima);
                    command.Parameters.AddWithValue("@NombreBeneficiario", (object)denuncia.NombreBeneficiario ?? DBNull.Value);
                    command.Parameters.AddWithValue("@CedulaBeneficiario", (object)denuncia.CedulaBeneficiario ?? DBNull.Value);
                    command.Parameters.AddWithValue("@MontoFraude", (object)denuncia.MontoFraude ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Banco", (object)denuncia.Banco ?? DBNull.Value);
                    command.Parameters.AddWithValue("@ProcedenciaDenuncia", denuncia.ProcedenciaDenuncia);
                    command.Parameters.AddWithValue("@PdfDenuncia", (object)denuncia.PdfDenuncia ?? DBNull.Value);
                    command.Parameters.AddWithValue("@UsuarioAsignadoId", (object)denuncia.UsuarioAsignadoId ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Estado", denuncia.Estado);
                    command.Parameters.AddWithValue("@FechaAsignacion", denuncia.FechaAsignacion ?? (object)DBNull.Value);
                    command.ExecuteNonQuery();
                }

                // Eliminar teléfonos antiguos y agregar nuevos
                query = "DELETE FROM TelefonosVictima WHERE DenunciaId = @DenunciaId";
                using (SqlCommand command = new SqlCommand(query, connection, transaction))
                {
                    command.Parameters.AddWithValue("@DenunciaId", denuncia.Id);
                    command.ExecuteNonQuery();
                }

                foreach (var telefono in denuncia.TelefonosVictima)
                {
                    query = "INSERT INTO TelefonosVictima (DenunciaId, Telefono) VALUES (@DenunciaId, @Telefono)";
                    using (SqlCommand command = new SqlCommand(query, connection, transaction))
                    {
                        command.Parameters.AddWithValue("@DenunciaId", denuncia.Id);
                        command.Parameters.AddWithValue("@Telefono", telefono);
                        command.ExecuteNonQuery();
                    }
                }

                // Eliminar teléfonos beneficiario antiguos y agregar nuevos
                query = "DELETE FROM TelefonosBeneficiario WHERE DenunciaId = @DenunciaId";
                using (SqlCommand command = new SqlCommand(query, connection, transaction))
                {
                    command.Parameters.AddWithValue("@DenunciaId", denuncia.Id);
                    command.ExecuteNonQuery();
                }

                if (denuncia.TelefonosBeneficiario != null)
                {
                    foreach (var telefono in denuncia.TelefonosBeneficiario)
                    {
                        query = "INSERT INTO TelefonosBeneficiario (DenunciaId, Telefono) VALUES (@DenunciaId, @Telefono)";
                        using (SqlCommand command = new SqlCommand(query, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@DenunciaId", denuncia.Id);
                            command.Parameters.AddWithValue("@Telefono", telefono);
                            command.ExecuteNonQuery();
                        }
                    }
                }

                // Eliminar cuentas antiguas y agregar nuevas
                query = "DELETE FROM CuentasBancarias WHERE DenunciaId = @DenunciaId";
                using (SqlCommand command = new SqlCommand(query, connection, transaction))
                {
                    command.Parameters.AddWithValue("@DenunciaId", denuncia.Id);
                    command.ExecuteNonQuery();
                }

                if (denuncia.CuentasBancarias != null)
                {
                    foreach (var cuenta in denuncia.CuentasBancarias)
                    {
                        query = "INSERT INTO CuentasBancarias (DenunciaId, NumeroCuenta, Banco) VALUES (@DenunciaId, @NumeroCuenta, @Banco)";
                        using (SqlCommand command = new SqlCommand(query, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@DenunciaId", denuncia.Id);
                            command.Parameters.AddWithValue("@NumeroCuenta", cuenta.NumeroCuenta);
                            command.Parameters.AddWithValue("@Banco", (object)cuenta.Banco ?? DBNull.Value);
                            command.ExecuteNonQuery();
                        }
                    }
                }

                // Registrar el cambio en el log
                query = "INSERT INTO LogsCambios (DenunciaId, UsuarioId, FechaCambio, CambioRealizado) VALUES (@DenunciaId, @UsuarioId, @FechaCambio, @CambioRealizado)";
                using (SqlCommand command = new SqlCommand(query, connection, transaction))
                {
                    command.Parameters.AddWithValue("@DenunciaId", denuncia.Id);
                    command.Parameters.AddWithValue("@UsuarioId", usuarioId);
                    command.Parameters.AddWithValue("@FechaCambio", DateTime.Now);
                    command.Parameters.AddWithValue("@CambioRealizado", cambios);
                    command.ExecuteNonQuery();
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }

    public List<Denuncia> BuscarDenunciasPorCedulaVictima(string cedula, Usuario usuarioActual)
    {
        string query = @"SELECT d.*, u.Nombre AS NombreUsuarioAsignado 
                   FROM Denuncias d
                   LEFT JOIN Usuarios u ON d.UsuarioAsignadoId = u.Id
                   WHERE d.CedulaVictima LIKE @Cedula";

        if (usuarioActual.Rol != "Admin")
        {
            query += " AND d.UsuarioCreadorId = @UsuarioId";
        }

        return EjecutarConsultaConFiltro(query, new SqlParameter("@Cedula", $"%{cedula}%"),
                                       usuarioActual.Rol != "Admin" ? new SqlParameter("@UsuarioId", usuarioActual.Id) : null);
    }

    public List<Denuncia> BuscarDenunciasPorTelefono(string telefono, Usuario usuarioActual)
    {
        string query = @"SELECT DISTINCT d.*, u.Nombre AS NombreUsuarioAsignado
                   FROM Denuncias d
                   LEFT JOIN Usuarios u ON d.UsuarioAsignadoId = u.Id
                   LEFT JOIN TelefonosVictima tv ON d.Id = tv.DenunciaId
                   LEFT JOIN TelefonosBeneficiario tb ON d.Id = tb.DenunciaId
                   WHERE (tv.Telefono LIKE @Telefono OR tb.Telefono LIKE @Telefono)";

        if (usuarioActual.Rol != "Admin")
        {
            query += " AND d.UsuarioCreadorId = @UsuarioId";
        }

        return EjecutarConsultaConFiltro(query, new SqlParameter("@Telefono", $"%{telefono}%"),
                                       usuarioActual.Rol != "Admin" ? new SqlParameter("@UsuarioId", usuarioActual.Id) : null);
    }

    public List<Denuncia> BuscarDenunciasPorBeneficiario(string beneficiario, Usuario usuarioActual)
    {
        string query = @"SELECT d.*, u.Nombre AS NombreUsuarioAsignado 
                   FROM Denuncias d
                   LEFT JOIN Usuarios u ON d.UsuarioAsignadoId = u.Id
                   WHERE (d.NombreBeneficiario LIKE @Beneficiario OR d.CedulaBeneficiario LIKE @Beneficiario)";

        if (usuarioActual.Rol != "Admin")
        {
            query += " AND d.UsuarioCreadorId = @UsuarioId";
        }

        return EjecutarConsultaConFiltro(query, new SqlParameter("@Beneficiario", $"%{beneficiario}%"),
                                       usuarioActual.Rol != "Admin" ? new SqlParameter("@UsuarioId", usuarioActual.Id) : null);
    }

    private List<Denuncia> EjecutarConsultaConFiltro(string query, params SqlParameter[] parameters)
    {
        List<Denuncia> denuncias = new List<Denuncia>();

        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            connection.Open();

            using (SqlCommand command = new SqlCommand(query, connection))
            {
                foreach (var param in parameters)
                {
                    if (param != null)
                        command.Parameters.Add(param);
                }

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Denuncia denuncia = new Denuncia
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            NumeroDenuncia = reader["NumeroDenuncia"].ToString(),
                            // ... (mapear el resto de campos como en ObtenerDenuncias)
                        };
                        denuncias.Add(denuncia);
                    }
                }
            }

            // Cargar datos relacionados (teléfonos, cuentas bancarias)
            foreach (var denuncia in denuncias)
            {
                denuncia.TelefonosVictima = ObtenerTelefonosVictima(denuncia.Id, connection);
                denuncia.TelefonosBeneficiario = ObtenerTelefonosBeneficiario(denuncia.Id, connection);
                denuncia.CuentasBancarias = ObtenerCuentasBancarias(denuncia.Id, connection);
            }
        }

        return denuncias;
    }





    public List<LogCambio> ObtenerLogsCambios(int denunciaId)
    {
        List<LogCambio> logs = new List<LogCambio>();

        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            connection.Open();

            string query = @"SELECT l.*, u.Nombre AS NombreUsuario 
                           FROM LogsCambios l
                           JOIN Usuarios u ON l.UsuarioId = u.Id
                           WHERE l.DenunciaId = @DenunciaId
                           ORDER BY l.FechaCambio DESC";

            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@DenunciaId", denunciaId);

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        logs.Add(new LogCambio
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            DenunciaId = Convert.ToInt32(reader["DenunciaId"]),
                            UsuarioId = Convert.ToInt32(reader["UsuarioId"]),
                            FechaCambio = Convert.ToDateTime(reader["FechaCambio"]),
                            CambioRealizado = reader["CambioRealizado"].ToString(),
                            NombreUsuario = reader["NombreUsuario"].ToString()
                        });
                    }
                }
            }
        }

        return logs;
    }

    public List<Usuario> ObtenerUsuarios()
    {
        List<Usuario> usuarios = new List<Usuario>();

        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            connection.Open();

            string query = "SELECT Id, Nombre, Email, Rol FROM Usuarios";

            using (SqlCommand command = new SqlCommand(query, connection))
            {
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        usuarios.Add(new Usuario
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            Nombre = reader["Nombre"].ToString(),
                            Email = reader["Email"].ToString(),
                            Rol = reader["Rol"].ToString()
                        });
                    }
                }
            }
        }

        return usuarios;
    }

    public DataTable GenerarReporte(string consulta, Dictionary<string, object> parametros = null)
    {
        DataTable table = new DataTable();

        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            connection.Open();

            using (SqlCommand command = new SqlCommand(consulta, connection))
            {
                if (parametros != null)
                {
                    foreach (var param in parametros)
                    {
                        command.Parameters.AddWithValue(param.Key, param.Value);
                    }
                }

                using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                {
                    adapter.Fill(table);
                }
            }
        }

        return table;
    }
}