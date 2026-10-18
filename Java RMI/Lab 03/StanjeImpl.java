/**
 * Konkretna implementacija stanja jednog korisnika u banci.
 *
 * Klasa je namerno jednostavna: samo cuva iznose i vraca ih preko get metoda.
 */
public class StanjeImpl implements Stanje {
    private static final long serialVersionUID = 1L;

    // Iznos na dinarskom racunu.
    private final float iznosDinarski;
    // Iznos na deviznom racunu.
    private final float iznosDevizni;

    /**
     * Pravi nepromenljiv snapshot oba racuna.
     */
    public StanjeImpl(float iznosDinarski, float iznosDevizni) {
        this.iznosDinarski = iznosDinarski;
        this.iznosDevizni = iznosDevizni;
    }

    @Override
    public float vratiDinarskiIznos() {
        return iznosDinarski;
    }

    @Override
    public float vratiDevizniIznos() {
        return iznosDevizni;
    }
}
