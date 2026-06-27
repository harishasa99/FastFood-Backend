namespace FastFoodApi.DTOs;

public class KorisnikRegisterDto
{
    public string Ime { get; set; } = "";
    public string Prezime { get; set; } = "";
    public string Email { get; set; } = "";
    public string Lozinka { get; set; } = "";
}

public class KorisnikLoginDto
{
    public string Email { get; set; } = "";
    public string Lozinka { get; set; } = "";
}

public class KonobarLoginDto
{
    public string Ime { get; set; } = "";
    public string Lozinka { get; set; } = "";
}

public class LoginResponseDto
{
    public string Token { get; set; } = "";
    public string Uloga { get; set; } = "";
    public string Ime { get; set; } = "";
    public string Id { get; set; } = "";
}

public class AzurirajProfilDto
{
    public string Ime { get; set; } = "";
    public string Prezime { get; set; } = "";
    public string Email { get; set; } = "";
    public string? StaraLozinka { get; set; }
    public string? NovaLozinka { get; set; }
}

public class ProizvodDto
{
    public string Naziv { get; set; } = "";
    public string Opis { get; set; } = "";
    public decimal Cena { get; set; }
}

public class KreirajRacunDto
{
    public Guid KorisnikId { get; set; }
    public List<RacunStavkaDto> Stavke { get; set; } = new();
}

public class RacunStavkaDto
{
    public Guid IdProizvoda { get; set; }
    public int Kolicina { get; set; }
}

public class RacunResponseDto
{
    public Guid Id { get; set; }
    public string Datum { get; set; } = "";
    public string Vreme { get; set; } = "";
    public decimal UkupnaCena { get; set; }
    public Guid KorisnikId { get; set; }
    public Guid KonobarId { get; set; }
    public List<RacunProizvodResponseDto> Proizvodi { get; set; } = new();
}

public class RacunProizvodResponseDto
{
    public Guid IdProizvoda { get; set; }
    public string Naziv { get; set; } = "";
    public decimal Cena { get; set; }
    public int Kolicina { get; set; }
    public decimal Ukupno => Cena * Kolicina;
}