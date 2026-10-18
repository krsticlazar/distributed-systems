import java.rmi.RemoteException;
import java.rmi.server.UnicastRemoteObject;

/**
 * Serverska implementacija jednog bankarskog korisnika.
 *
 * Ovaj objekat cuva identitet korisnika i stanje oba racuna. Sve promene
 * stanja obavljaju se ovde, na serverskoj strani.
 */
public class KorisnikImpl extends UnicastRemoteObject implements Korisnik {
    // Jedinstveni broj korisnika.
    private final String jbk;
    // Trenutni iznos na dinarskom racunu.
    private float iznosDinarski;
    // Trenutni iznos na deviznom racunu.
    private float iznosDevizni;

    /**
     * Konstruktor priprema korisnika sa pocetnim stanjima racuna.
     */
    public KorisnikImpl(String jbk, float iznosDinarski, float iznosDevizni) throws RemoteException {
        super();
        this.jbk = jbk;
        this.iznosDinarski = iznosDinarski;
        this.iznosDevizni = iznosDevizni;
    }

    /**
     * Pomocna metoda koja moze biti korisna za kasniji debugging ili ispis.
     */
    public String vratiJbk() {
        return jbk;
    }

    @Override
    public synchronized Stanje vratiStanje() throws RemoteException {
        // Klijentu vracamo snapshot, ne direktan pristup poljima.
        return new StanjeImpl(iznosDinarski, iznosDevizni);
    }

    @Override
    public synchronized void transferDinarskiNaDevizni(float iznos, float kurs) throws RemoteException {
        // Transfer vazi samo za smislen unos i ako na dinarskom racunu ima dovoljno novca.
        if (iznos <= 0 || kurs <= 0 || iznos > iznosDinarski) {
            return;
        }

        // Sa dinarskog skidamo trazeni iznos, a na devizni dodajemo preracunatu vrednost.
        iznosDinarski -= iznos;
        iznosDevizni += iznos / kurs;
    }

    @Override
    public synchronized void transferDevizniNaDinarski(float iznos, float kurs) throws RemoteException {
        // Ovde je "iznos" izrazem u devizama, pa proveravamo devizni racun.
        if (iznos <= 0 || kurs <= 0 || iznos > iznosDevizni) {
            return;
        }

        // Devize skidamo, a na dinarski racun dodajemo vrednost pomnozenu kursom.
        iznosDevizni -= iznos;
        iznosDinarski += iznos * kurs;
    }
}
