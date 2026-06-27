using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FastFoodApi.DTOs;
using FastFoodApi.Models;
using FastFoodApi.Services;

namespace FastFoodApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProizvodController : ControllerBase
{
    private readonly CassandraService _cassandra;

    public ProizvodController(CassandraService cassandra)
    {
        _cassandra = cassandra;
    }

    [HttpGet]
    [Authorize]
    public IActionResult GetSvi()
    {
        var result = _cassandra.Session.Execute("SELECT * FROM proizvod");

        var lista = result.Select(r => new Proizvod
        {
            Id = r.GetValue<Guid>("id"),
            Naziv = r.GetValue<string>("naziv"),
            Opis = r.GetValue<string>("opis"),
            Cena = r.GetValue<decimal>("cena")
        }).ToList();

        return Ok(lista);
    }

    [HttpGet("{id}")]
    [Authorize]
    public IActionResult GetById(Guid id)
    {
        var ps = _cassandra.Session.Prepare("SELECT * FROM proizvod WHERE id = ?");
        var row = _cassandra.Session.Execute(ps.Bind(id)).FirstOrDefault();

        if (row == null) return NotFound(new { poruka = "Proizvod nije pronadjen." });

        return Ok(new Proizvod
        {
            Id = row.GetValue<Guid>("id"),
            Naziv = row.GetValue<string>("naziv"),
            Opis = row.GetValue<string>("opis"),
            Cena = row.GetValue<decimal>("cena")
        });
    }

    [HttpPost]
    [Authorize(Roles = "konobar")]
    public IActionResult Kreiraj([FromBody] ProizvodDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Naziv) || dto.Cena <= 0)
            return BadRequest(new { poruka = "Naziv i cena su obavezni." });

        var id = Guid.NewGuid();
        var ps = _cassandra.Session.Prepare(
            "INSERT INTO proizvod (id, naziv, opis, cena) VALUES (?, ?, ?, ?)");
        _cassandra.Session.Execute(ps.Bind(id, dto.Naziv, dto.Opis, dto.Cena));

        return CreatedAtAction(nameof(GetById), new { id },
            new { id, dto.Naziv, dto.Opis, dto.Cena });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "konobar")]
    public IActionResult Azuriraj(Guid id, [FromBody] ProizvodDto dto)
    {
        var ps = _cassandra.Session.Prepare(
            "UPDATE proizvod SET naziv = ?, opis = ?, cena = ? WHERE id = ?");
        _cassandra.Session.Execute(ps.Bind(dto.Naziv, dto.Opis, dto.Cena, id));

        return Ok(new { poruka = "Proizvod azuriran." });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "konobar")]
    public IActionResult Obrisi(Guid id)
    {
        var ps = _cassandra.Session.Prepare("DELETE FROM proizvod WHERE id = ?");
        _cassandra.Session.Execute(ps.Bind(id));

        return Ok(new { poruka = "Proizvod obrisan." });
    }
}