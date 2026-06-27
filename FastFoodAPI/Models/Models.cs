namespace FastFoodApi.Models;

public class Korisnik
{
    public Guid Id { get; set; }
    public string Ime { get; set; } = "";
    public string Prezime { get; set; } = "";
    public string Email { get; set; } = "";
    public string Lozinka { get; set; } = "";
}

public class Konobar
{
    public Guid Id { get; set; }
    public string Ime { get; set; } = "";
    public string Prezime { get; set; } = "";
    public string Lozinka { get; set; } = "";
}

public class Proizvod
{
    public Guid Id { get; set; }
    public string Naziv { get; set; } = "";
    public string Opis { get; set; } = "";
    public decimal Cena { get; set; }
}

public class Racun
{
    public Guid Id { get; set; }
    public string Datum { get; set; } = "";
    public string Vreme { get; set; } = "";
    public decimal UkupnaCena { get; set; }
    public Guid KorisnikId { get; set; }
    public Guid KonobarId { get; set; }
    public List<RacunProizvod> Proizvodi { get; set; } = new();
}

public class RacunProizvod
{
    public Guid IdRacuna { get; set; }
    public Guid IdProizvoda { get; set; }
    public int Kolicina { get; set; }
    public string? NazivProizvoda { get; set; }
    public decimal? CenaProizvoda { get; set; }
}