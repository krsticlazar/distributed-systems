import java.io.Serializable;

/**
 * Snapshot trenutnog stanja jednog korisnika.
 *
 * Stanje nije udaljeni RMI objekat. Klijent ga dobija kao serijalizovan paket
 * podataka sa servera i koristi samo za citanje i prikaz na konzoli.
 */
public interface Stanje extends Serializable {
    /**
     * Vraca broj preostalih minuta.
     */
    int vratiMinute();

    /**
     * Vraca broj preostalih poruka.
     */
    int vratiPoruke();

    /**
     * Vraca broj preostalih megabajta interneta.
     */
    int vratiInternet();

    /**
     * Vraca trenutni iznos racuna.
     */
    float vratiRacun();
}
