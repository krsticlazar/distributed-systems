import java.io.Serializable;

/**
 * Snapshot stanja korisnickih racuna u jednom trenutku.
 *
 * Ovo nije udaljeni objekat. Server salje stanje klijentu kao serijalizovan
 * objekat, a klijent ga koristi samo za ispis.
 */
public interface Stanje extends Serializable {
    /**
     * Vraca trenutno stanje na dinarskom racunu.
     */
    float vratiDinarskiIznos();

    /**
     * Vraca trenutno stanje na deviznom racunu.
     */
    float vratiDevizniIznos();
}
