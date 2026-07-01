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
        if (adminKey != "FastFood_Admin_Secret_2026")
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
    
}

public class DodajKonobaraDto
{
    public string Ime { get; set; } = "";
    public string Prezime { get; set; } = "";
    public string Lozinka { get; set; } = "";
}