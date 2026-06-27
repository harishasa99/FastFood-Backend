using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using FastFoodApi.Services;
using BC = BCrypt.Net.BCrypt;

namespace FastFoodApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class KonobarController : ControllerBase
{
    private readonly CassandraService _cassandra;

    public KonobarController(CassandraService cassandra)
    {
        _cassandra = cassandra;
    }

    // Ubacivanje konobara - zaštićeno admin ključem u headeru
    [HttpPost("dodaj")]
    public IActionResult DodajKonobara(
        [FromBody] DodajKonobaraDto dto,
        [FromHeader(Name = "X-Admin-Key")] string? adminKey)
    {
        if (adminKey != "FastFood_Admin_Secret_2024")
            return Unauthorized(new { poruka = "Neautorizovan pristup." });

        if (string.IsNullOrWhiteSpace(dto.Ime) || string.IsNullOrWhiteSpace(dto.Lozinka))
            return BadRequest(new { poruka = "Ime i lozinka su obavezni." });

        var id = Guid.NewGuid();
        var hashedLozinka = BC.HashPassword(dto.Lozinka);

        var ps = _cassandra.Session.Prepare(
            "INSERT INTO konobar (id, ime, prezime, lozinka) VALUES (?, ?, ?, ?)");
        _cassandra.Session.Execute(ps.Bind(id, dto.Ime, dto.Prezime, hashedLozinka));

        return Ok(new { poruka = "Konobar uspjesno dodat.", id });
    }

    [HttpGet("profil")]
    [Authorize(Roles = "konobar")]
    public IActionResult GetProfil()
    {
        var id = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var ps = _cassandra.Session.Prepare(
            "SELECT id, ime, prezime FROM konobar WHERE id = ?");
        var row = _cassandra.Session.Execute(ps.Bind(id)).FirstOrDefault();

        if (row == null) return NotFound(new { poruka = "Konobar nije pronadjen." });

        return Ok(new
        {
            Id = row.GetValue<Guid>("id"),
            Ime = row.GetValue<string>("ime"),
            Prezime = row.GetValue<string>("prezime")
        });
    }

    [HttpGet("svi")]
    [Authorize(Roles = "konobar")]
    public IActionResult SviKonobari()
    {
        var result = _cassandra.Session.Execute(
            "SELECT id, ime, prezime FROM konobar");

        var lista = result.Select(r => new
        {
            Id = r.GetValue<Guid>("id"),
            Ime = r.GetValue<string>("ime"),
            Prezime = r.GetValue<string>("prezime")
        }).ToList();

        return Ok(lista);
    }
}

public class DodajKonobaraDto
{
    public string Ime { get; set; } = "";
    public string Prezime { get; set; } = "";
    public string Lozinka { get; set; } = "";
}