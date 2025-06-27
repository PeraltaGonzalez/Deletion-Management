using System;
public class LogCambio
{
    public int Id { get; set; }
    public int DenunciaId { get; set; }
    public int UsuarioId { get; set; }
    public DateTime FechaCambio { get; set; }
    public string CambioRealizado { get; set; }
    public string NombreUsuario { get; set; }
}