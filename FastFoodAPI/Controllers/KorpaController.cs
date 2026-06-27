using Cassandra;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;
using FastFoodApi.Services;

namespace FastFoodApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class KorpaController : ControllerBase
{
    private readonly CassandraService _cassandra;

    public KorpaController(CassandraService cassandra)
    {
        _cassandra = cassandra;
    }

    private Guid TrenutniId =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    // =============================================
    // KORISNIK: Sačuvaj korpu
    // Korisnik odabere proizvode i pošalje korpu
    // =============================================
    [HttpPost]
    [Authorize(Roles = "korisnik")]
    public IActionResult SacuvajKorpu([FromBody] List<KorpaStavkaDto> stavke)
    {
        var session = _cassandra.Session;
        var korisnikId = TrenutniId;
        var json = JsonSerializer.Serialize(stavke);

        var ps = session.Prepare(
            "INSERT INTO korpa (korisnik_id, stavke, datum_azuriranja) VALUES (?, ?, ?)");
        session.Execute(ps.Bind(korisnikId, json, DateTimeOffset.UtcNow));

        return Ok(new { poruka = "Narudzba poslata konobaru!" });
    }

    // =============================================
    // KORISNIK: Dohvati svoju korpu
    // =============================================
    [HttpGet("moja")]
    [Authorize(Roles = "korisnik")]
    public IActionResult MojaKorpa()
    {
        var session = _cassandra.Session;
        var korisnikId = TrenutniId;

        var ps = session.Prepare("SELECT stavke FROM korpa WHERE korisnik_id = ?");
        var row = session.Execute(ps.Bind(korisnikId)).FirstOrDefault();

        if (row == null) return Ok(new List<object>());

        var json = row.GetValue<string>("stavke");
        var stavke = JsonSerializer.Deserialize<List<KorpaStavkaDto>>(json);

        return Ok(stavke);
    }

    // =============================================
    // KONOBAR: Dohvati korpu određenog korisnika
    // Konobar klikne na korisnika i vidi šta je naručio
    // =============================================
    [HttpGet("korisnik/{korisnikId}")]
    [Authorize(Roles = "konobar")]
    public IActionResult KorpaKorisnika(Guid korisnikId)
    {
        var session = _cassandra.Session;

        var ps = session.Prepare("SELECT stavke, datum_azuriranja FROM korpa WHERE korisnik_id = ?");
        var row = session.Execute(ps.Bind(korisnikId)).FirstOrDefault();

        if (row == null) return Ok(new { stavke = new List<object>(), imaKorpu = false });

        var json = row.GetValue<string>("stavke");
        var stavke = JsonSerializer.Deserialize<List<KorpaStavkaDto>>(json);
        var datum = row.GetValue<DateTimeOffset>("datum_azuriranja");

        return Ok(new { stavke, imaKorpu = true, datum = datum.ToString("yyyy-MM-dd HH:mm") });
    }

    // =============================================
    // KONOBAR: Obrisi korpu nakon izdavanja racuna
    // =============================================
    [HttpDelete("korisnik/{korisnikId}")]
    [Authorize(Roles = "konobar")]
    public IActionResult ObrisiKorpu(Guid korisnikId)
    {
        var ps = _cassandra.Session.Prepare(
            "DELETE FROM korpa WHERE korisnik_id = ?");
        _cassandra.Session.Execute(ps.Bind(korisnikId));

        return Ok(new { poruka = "Korpa obrisana." });
    }
}

public class KorpaStavkaDto
{
    public string IdProizvoda { get; set; } = "";
    public string Naziv { get; set; } = "";
    public decimal Cena { get; set; }
    public int Kolicina { get; set; }
}