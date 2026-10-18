/**
 * Konkretna implementacija objekta koji opisuje trenutno stanje korisnika.
 *
 * Ova klasa sadrzi samo podatke i get metode. Ne sadrzi poslovnu logiku.
 */
public class StanjeImpl implements Stanje {
    private static final long serialVersionUID = 1L;

    // Broj telefona za koji je formiran ovaj snapshot.
    private final String broj;
    // Trenutni broj minuta.
    private final int minuti;
    // Trenutni broj poruka.
    private final int poruke;
    // Trenutni broj MB interneta.
    private final int internet;
    // Trenutni obracunati racun.
    private final float racun;

    /**
     * Konstruktor pravi nepromenljiv prikaz stanja u datom trenutku.
     */
    public StanjeImpl(String broj, int minuti, int poruke, int internet, float racun) {
        this.broj = broj;
        this.minuti = minuti;
        this.poruke = poruke;
        this.internet = internet;
        this.racun = racun;
    }

    /**
     * Dodatna pomocna metoda. Nije trazena interfejsom, ali je korisna ako
     * nekad pozelis da broj telefona prikazes uz stanje.
     */
    public String vratiBroj() {
        return broj;
    }

    @Override
    public int vratiMinute() {
        return minuti;
    }

    @Override
    public int vratiPoruke() {
        return poruke;
    }

    @Override
    public int vratiInternet() {
        return internet;
    }

    @Override
    public float vratiRacun() {
        return racun;
    }
}
