using System;
using System.Collections.Generic;

public class Denuncia
{
    public int Id { get; set; }
    public string NumeroDenuncia { get; set; }
    public DateTime FechaRegistro { get; set; }
    public DateTime FechaEvento { get; set; }
    public string TipoDelito { get; set; }
    public string DotacionPolicial { get; set; }
    public string Provincia { get; set; }
    public string Municipio { get; set; }
    public string NombreVictima { get; set; }
    public string CedulaVictima { get; set; }
    public List<string> TelefonosVictima { get; set; } = new List<string>();
    public string NombreBeneficiario { get; set; }
    public string CedulaBeneficiario { get; set; }
    public List<string> TelefonosBeneficiario { get; set; }
    public decimal? MontoFraude { get; set; }
    public List<CuentaBancaria> CuentasBancarias { get; set; }
    public string Banco { get; set; }
    public string ProcedenciaDenuncia { get; set; }
    public byte[] PdfDenuncia { get; set; }
    public int? UsuarioAsignadoId { get; set; }
    public string Estado { get; set; }
    public DateTime? FechaAsignacion { get; set; }
    public int UsuarioCreadorId { get; set; }
    public string NombreUsuarioAsignado { get; set; }
}