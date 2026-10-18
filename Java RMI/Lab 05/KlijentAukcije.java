import java.io.Serializable;

public class KlijentAukcije implements Serializable {                  // RMI ovaj objekat salje serveru po vrednosti.
    private static final long serialVersionUID = 1L;                   // Verzija Serializable klase.

    private final String klijentAukcijeId;                             
    private final String ime;                                          
    private final String prezime;                                      

    public KlijentAukcije(String klijentAukcijeId, String ime, String prezime) {   // Lokalni objekat koji klijent pravi pre slanja serveru.
        this.klijentAukcijeId = klijentAukcijeId;
        this.ime = ime;
        this.prezime = prezime;
    }

    public String vratiKlijentAukcijeId() {
        return klijentAukcijeId;
    }

    public String vratiIme() {
        return ime;
    }

    public String vratiPrezime() {
        return prezime;
    }

    @Override
    public String toString() {                                        
        return klijentAukcijeId + " - " + ime + " " + prezime;
    }
}
