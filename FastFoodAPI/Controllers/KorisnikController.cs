using Cassandra;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using FastFoodApi.DTOs;
using FastFoodApi.Services;
using BC = BCrypt.Net.BCrypt;

namespace FastFoodApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class KorisnikController : ControllerBase
{
    private readonly CassandraService _cassandra;

    public KorisnikController(CassandraService cassandra)
    {
        _cassandra = cassandra;
    }

    private Guid TrenutniId =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    [HttpGet("profil")]
    [Authorize(Roles = "korisnik")]
    public IActionResult GetProfil()
    {
        var ps = _cassandra.Session.Prepare(
            "SELECT id, ime, prezime, email FROM korisnik WHERE id = ?");
        var row = _cassandra.Session.Execute(ps.Bind(TrenutniId)).FirstOrDefault();

        if (row == null) return NotFound(new { poruka = "Korisnik nije pronadjen." });

        return Ok(new
        {
            Id = row.GetValue<Guid>("id"),
            Ime = row.GetValue<string>("ime"),
            Prezime = row.GetValue<string>("prezime"),
            Email = row.GetValue<string>("email")
        });
    }

    [HttpPut("profil")]
    [Authorize(Roles = "korisnik")]
    public IActionResult AzurirajProfil([FromBody] AzurirajProfilDto dto)
    {
        var session = _cassandra.Session;

        if (!string.IsNullOrWhiteSpace(dto.NovaLozinka))
        {
            if (string.IsNullOrWhiteSpace(dto.StaraLozinka))
                return BadRequest(new { poruka = "Unesite staru lozinku." });

            var checkPs = session.Prepare(
                "SELECT lozinka FROM korisnik WHERE id = ?");
            var row = session.Execute(checkPs.Bind(TrenutniId)).FirstOrDefault();

            if (row == null || !BC.Verify(dto.StaraLozinka, row.GetValue<string>("lozinka")))
                return BadRequest(new { poruka = "Stara lozinka nije tacna." });

            var novaHash = BC.HashPassword(dto.NovaLozinka);
            var ps = session.Prepare(
                "UPDATE korisnik SET ime = ?, prezime = ?, email = ?, lozinka = ? WHERE id = ?");
            session.Execute(ps.Bind(dto.Ime, dto.Prezime, dto.Email, novaHash, TrenutniId));
        }
        else
        {
            var ps = session.Prepare(
                "UPDATE korisnik SET ime = ?, prezime = ?, email = ? WHERE id = ?");
            session.Execute(ps.Bind(dto.Ime, dto.Prezime, dto.Email, TrenutniId));
        }

        return Ok(new { poruka = "Profil uspjesno azuriran." });
    }

    [HttpGet("svi")]
    [Authorize(Roles = "konobar")]
    public IActionResult SviKorisnici()
    {
        var result = _cassandra.Session.Execute(
            "SELECT id, ime, prezime, email FROM korisnik");

        var lista = result.Select(r => new
        {
            Id = r.GetValue<Guid>("id"),
            Ime = r.GetValue<string>("ime"),
            Prezime = r.GetValue<string>("prezime"),
            Email = r.GetValue<string>("email")
        }).ToList();

        return Ok(lista);
    }


}