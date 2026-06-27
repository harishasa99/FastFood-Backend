using Cassandra;
using Microsoft.AspNetCore.Mvc;
using FastFoodApi.DTOs;
using FastFoodApi.Services;
using BC = BCrypt.Net.BCrypt;

namespace FastFoodApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly CassandraService _cassandra;
    private readonly TokenService _tokenService;

    public AuthController(CassandraService cassandra, TokenService tokenService)
    {
        _cassandra = cassandra;
        _tokenService = tokenService;
    }

    [HttpPost("registracija")]
    public IActionResult Registracija([FromBody] KorisnikRegisterDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Ime) ||
            string.IsNullOrWhiteSpace(dto.Email) ||
            string.IsNullOrWhiteSpace(dto.Lozinka))
            return BadRequest(new { poruka = "Sva polja su obavezna." });

        var session = _cassandra.Session;

        var checkPs = session.Prepare(
            "SELECT id FROM korisnik WHERE email = ? ALLOW FILTERING");
        var postoji = session.Execute(checkPs.Bind(dto.Email)).FirstOrDefault();
        if (postoji != null)
            return Conflict(new { poruka = "Email vec postoji." });

        var id = Guid.NewGuid();
        var hashedLozinka = BC.HashPassword(dto.Lozinka);

        var ps = session.Prepare(
            "INSERT INTO korisnik (id, ime, prezime, email, lozinka) VALUES (?, ?, ?, ?, ?)");
        session.Execute(ps.Bind(id, dto.Ime, dto.Prezime, dto.Email, hashedLozinka));

        return Ok(new { poruka = "Registracija uspjesna.", id });
    }

    [HttpPost("prijava/korisnik")]
    public IActionResult PrijavaKorisnik([FromBody] KorisnikLoginDto dto)
    {
        var session = _cassandra.Session;

        var ps = session.Prepare(
            "SELECT id, ime, prezime, email, lozinka FROM korisnik WHERE email = ? ALLOW FILTERING");
        var row = session.Execute(ps.Bind(dto.Email)).FirstOrDefault();

        if (row == null || !BC.Verify(dto.Lozinka, row.GetValue<string>("lozinka")))
            return Unauthorized(new { poruka = "Pogresan email ili lozinka." });

        var id = row.GetValue<Guid>("id");
        var ime = row.GetValue<string>("ime");
        var token = _tokenService.GenerisiToken(id, ime, "korisnik");

        return Ok(new LoginResponseDto
        {
            Token = token,
            Uloga = "korisnik",
            Ime = ime,
            Id = id.ToString()
        });
    }

    [HttpPost("prijava/konobar")]
    public IActionResult PrijavaKonobar([FromBody] KonobarLoginDto dto)
    {
        var session = _cassandra.Session;

        var sviKonobari = session.Execute(
            "SELECT id, ime, prezime, lozinka FROM konobar");

        Row? nadjen = null;
        foreach (var row in sviKonobari)
        {
            if (row.GetValue<string>("ime") == dto.Ime &&
                BC.Verify(dto.Lozinka, row.GetValue<string>("lozinka")))
            {
                nadjen = row;
                break;
            }
        }

        if (nadjen == null)
            return Unauthorized(new { poruka = "Pogresno ime ili lozinka." });

        var id = nadjen.GetValue<Guid>("id");
        var ime = nadjen.GetValue<string>("ime");
        var token = _tokenService.GenerisiToken(id, ime, "konobar");

        return Ok(new LoginResponseDto
        {
            Token = token,
            Uloga = "konobar",
            Ime = ime,
            Id = id.ToString()
        });
    }
}