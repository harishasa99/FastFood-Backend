using Cassandra;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using FastFoodApi.DTOs;
using FastFoodApi.Services;

namespace FastFoodApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RacunController : ControllerBase
{
    private readonly CassandraService _cassandra;

    public RacunController(CassandraService cassandra)
    {
        _cassandra = cassandra;
    }

    private Guid TrenutniId =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    [HttpPost]
    [Authorize(Roles = "konobar")]
    public IActionResult KreirajRacun([FromBody] KreirajRacunDto dto)
    {
        if (dto.Stavke == null || !dto.Stavke.Any())
            return BadRequest(new { poruka = "Racun mora imati bar jedan proizvod." });

        var session = _cassandra.Session;
        var konobarId = TrenutniId;
        var racunId = Guid.NewGuid();
        var sada = DateTime.UtcNow;
        decimal ukupnaCena = 0;

        var korisnikPs = session.Prepare("SELECT id FROM korisnik WHERE id = ?");
        var korisnikRow = session.Execute(korisnikPs.Bind(dto.KorisnikId)).FirstOrDefault();
        if (korisnikRow == null)
            return NotFound(new { poruka = "Korisnik nije pronadjen." });

        var stavkeZaUbaciti = new List<(Guid idProizvoda, int kolicina, decimal cena, string naziv)>();

        foreach (var stavka in dto.Stavke)
        {
            var pPs = session.Prepare("SELECT id, naziv, cena FROM proizvod WHERE id = ?");
            var pRow = session.Execute(pPs.Bind(stavka.IdProizvoda)).FirstOrDefault();

            if (pRow == null)
                return NotFound(new { poruka = $"Proizvod {stavka.IdProizvoda} nije pronadjen." });

            var cena = pRow.GetValue<decimal>("cena");
            var naziv = pRow.GetValue<string>("naziv");
            ukupnaCena += cena * stavka.Kolicina;
            stavkeZaUbaciti.Add((stavka.IdProizvoda, stavka.Kolicina, cena, naziv));
        }

        var racunPs = session.Prepare(
            "INSERT INTO racun (id, datum, vreme, ukupna_cena, korisnik_id, konobar_id) " +
            "VALUES (?, ?, ?, ?, ?, ?)");
        session.Execute(racunPs.Bind(
            racunId,
            new LocalDate(sada.Year, sada.Month, sada.Day),
            new LocalTime(sada.Hour, sada.Minute, sada.Second, 0),
            ukupnaCena,
            dto.KorisnikId,
            konobarId));

        var stavkaPs = session.Prepare(
            "INSERT INTO racun_proiz (id_racuna, id_proizvoda, kolicina) VALUES (?, ?, ?)");
        foreach (var (idP, kol, _, _) in stavkeZaUbaciti)
        {
            session.Execute(stavkaPs.Bind(racunId, idP, kol));
        }

        return Ok(new RacunResponseDto
        {
            Id = racunId,
            Datum = sada.ToString("yyyy-MM-dd"),
            Vreme = sada.ToString("HH:mm:ss"),
            UkupnaCena = ukupnaCena,
            KorisnikId = dto.KorisnikId,
            KonobarId = konobarId,
            Proizvodi = stavkeZaUbaciti.Select(s => new RacunProizvodResponseDto
            {
                IdProizvoda = s.idProizvoda,
                Naziv = s.naziv,
                Cena = s.cena,
                Kolicina = s.kolicina
            }).ToList()
        });
    }

    [HttpGet("moji")]
    [Authorize(Roles = "korisnik")]
    public IActionResult MojiRacuni()
    {
        var session = _cassandra.Session;
        var korisnikId = TrenutniId;

        var ps = session.Prepare(
            "SELECT id, datum, vreme, ukupna_cena, konobar_id FROM racun " +
            "WHERE korisnik_id = ? ALLOW FILTERING");
        var racuni = session.Execute(ps.Bind(korisnikId));

        var lista = new List<object>();
        foreach (var r in racuni)
        {
            var racunId = r.GetValue<Guid>("id");

            var stavkePs = session.Prepare(
                "SELECT id_proizvoda, kolicina FROM racun_proiz WHERE id_racuna = ?");
            var stavkeRows = session.Execute(stavkePs.Bind(racunId));

            var stavke = new List<object>();
            foreach (var s in stavkeRows)
            {
                var idP = s.GetValue<Guid>("id_proizvoda");
                var kol = s.GetValue<int>("kolicina");

                var pPs = session.Prepare("SELECT naziv, cena FROM proizvod WHERE id = ?");
                var pRow = session.Execute(pPs.Bind(idP)).FirstOrDefault();

                stavke.Add(new
                {
                    IdProizvoda = idP,
                    Naziv = pRow?.GetValue<string>("naziv") ?? "N/A",
                    Cena = pRow?.GetValue<decimal>("cena") ?? 0,
                    Kolicina = kol
                });
            }

            lista.Add(new
            {
                Id = racunId,
                Datum = r.GetValue<LocalDate>("datum").ToString(),
                Vreme = r.GetValue<LocalTime>("vreme").ToString(),
                UkupnaCena = r.GetValue<decimal>("ukupna_cena"),
                KonobarId = r.GetValue<Guid>("konobar_id"),
                Stavke = stavke
            });
        }

        return Ok(lista);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "korisnik")]
    public IActionResult GetRacun(Guid id)
    {
        var session = _cassandra.Session;

        var ps = session.Prepare("SELECT * FROM racun WHERE id = ?");
        var row = session.Execute(ps.Bind(id)).FirstOrDefault();

        if (row == null) return NotFound(new { poruka = "Racun nije pronadjen." });

        if (row.GetValue<Guid>("korisnik_id") != TrenutniId)
            return Forbid();

        var stavkePs = session.Prepare(
            "SELECT id_proizvoda, kolicina FROM racun_proiz WHERE id_racuna = ?");
        var stavkeRows = session.Execute(stavkePs.Bind(id));

        var stavke = new List<object>();
        foreach (var s in stavkeRows)
        {
            var idP = s.GetValue<Guid>("id_proizvoda");
            var kol = s.GetValue<int>("kolicina");

            var pPs = session.Prepare("SELECT naziv, cena FROM proizvod WHERE id = ?");
            var pRow = session.Execute(pPs.Bind(idP)).FirstOrDefault();

            stavke.Add(new
            {
                IdProizvoda = idP,
                Naziv = pRow?.GetValue<string>("naziv") ?? "N/A",
                Cena = pRow?.GetValue<decimal>("cena") ?? 0,
                Kolicina = kol,
                Ukupno = (pRow?.GetValue<decimal>("cena") ?? 0) * kol
            });
        }

        return Ok(new
        {
            Id = id,
            Datum = row.GetValue<LocalDate>("datum").ToString(),
            Vreme = row.GetValue<LocalTime>("vreme").ToString(),
            UkupnaCena = row.GetValue<decimal>("ukupna_cena"),
            KorisnikId = row.GetValue<Guid>("korisnik_id"),
            KonobarId = row.GetValue<Guid>("konobar_id"),
            Stavke = stavke
        });
    }

    [HttpGet("svi")]
    [Authorize(Roles = "konobar")]
    public IActionResult SviRacuni()
    {
        var result = _cassandra.Session.Execute("SELECT * FROM racun");

        var lista = result.Select(r => new
        {
            Id = r.GetValue<Guid>("id"),
            Datum = r.GetValue<LocalDate>("datum").ToString(),
            Vreme = r.GetValue<LocalTime>("vreme").ToString(),
            UkupnaCena = r.GetValue<decimal>("ukupna_cena"),
            KorisnikId = r.GetValue<Guid>("korisnik_id"),
            KonobarId = r.GetValue<Guid>("konobar_id")
        }).ToList();

        return Ok(lista);
    }

    [HttpGet("korisnik/{korisnikId}")]
    [Authorize(Roles = "konobar")]
    public IActionResult RacuniZaKorisnika(Guid korisnikId)
    {
        var ps = _cassandra.Session.Prepare(
            "SELECT * FROM racun WHERE korisnik_id = ? ALLOW FILTERING");
        var result = _cassandra.Session.Execute(ps.Bind(korisnikId));

        var lista = result.Select(r => new
        {
            Id = r.GetValue<Guid>("id"),
            Datum = r.GetValue<LocalDate>("datum").ToString(),
            Vreme = r.GetValue<LocalTime>("vreme").ToString(),
            UkupnaCena = r.GetValue<decimal>("ukupna_cena"),
            KonobarId = r.GetValue<Guid>("konobar_id")
        }).ToList();

        return Ok(lista);
    }
}