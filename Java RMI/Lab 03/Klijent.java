import java.rmi.Naming;
import java.text.DecimalFormat;
import java.util.Scanner;

/**
 * Klijentska konzolna aplikacija za eBank servis.
 *
 * Klijent ne racuna stanja i ne radi transfere lokalno. On samo prikuplja
 * unos od korisnika i prosledjuje zahteve udaljenim objektima.
 */
public class Klijent {
    // Za jednostavnost primera koristi se fiksan kurs za sve transfere.
    private static final float KURS = 117.0f;
    // Format za uredan prikaz decimalnih vrednosti.
    private static final DecimalFormat FORMAT = new DecimalFormat("0.00");

    public static void main(String[] args) {
        try (Scanner scanner = new Scanner(System.in)) {
            // Glavni udaljeni objekat banke.
            EBanka eBanka = (EBanka) Naming.lookup("rmi://localhost:1099/EBanka");

            while (true) {
                ispisiMeni();
                String opcija = scanner.nextLine().trim().toLowerCase();

                // "d" zatvara klijentsku aplikaciju.
                if (opcija.equals("d")) {
                    break;
                }

                // Sve ostale vrednosti van menija odbacujemo.
                if (!opcija.equals("a") && !opcija.equals("b") && !opcija.equals("c")) {
                    System.out.println("Nepoznata opcija.");
                    System.out.println();
                    continue;
                }

                if (opcija.equals("a")) {
                    System.out.println("Izabrali ste opciju za transfer sa dinarskog na devizni racun:");
                } else if (opcija.equals("b")) {
                    System.out.println("Izabrali ste opciju za transfer sa deviznog na dinarski racun:");
                } else {
                    System.out.println("Izabrali ste opciju za proveru stanja:");
                }

                System.out.println("Unesite jedinstveni broj korisnika:");
                String jbk = scanner.nextLine().trim();
                // Preko banke trazimo konkretan udaljeni objekat korisnika.
                Korisnik korisnik = eBanka.vratiKorisnika(jbk);

                if (korisnik == null) {
                    System.out.println("Korisnik sa zadatim brojem ne postoji.");
                    System.out.println();
                    continue;
                }

                if (opcija.equals("a")) {
                    float iznos = procitajPozitivanIznos(scanner, "Unesite iznos:");
                    // Sav obracun transfera obavlja server.
                    korisnik.transferDinarskiNaDevizni(iznos, KURS);
                    System.out.println("Transfer je evidentiran po kursu " + formatiraj(KURS) + ".");
                    System.out.println();
                } else if (opcija.equals("b")) {
                    float iznos = procitajPozitivanIznos(scanner, "Unesite iznos:");
                    korisnik.transferDevizniNaDinarski(iznos, KURS);
                    System.out.println("Transfer je evidentiran po kursu " + formatiraj(KURS) + ".");
                    System.out.println();
                } else {
                    // Za pregled stanja dobijamo serijalizovan snapshot.
                    Stanje stanje = korisnik.vratiStanje();
                    System.out.println("Vase stanje je:");
                    System.out.println("Iznos na dinarskom racunu: " + formatiraj(stanje.vratiDinarskiIznos()));
                    System.out.println("Iznos na deviznom racunu: " + formatiraj(stanje.vratiDevizniIznos()));
                    System.out.println();
                }
            }
        } catch (Exception e) {
            e.printStackTrace();
        }
    }

    private static void ispisiMeni() {
        // Izdvojeno zbog preglednosti glavne petlje.
        System.out.println("Dobrodosli u eBank korisnicki servis. Za nastavak izaberite opciju:");
        System.out.println("a) Transfer sa dinarskog na devizni racun");
        System.out.println("b) Transfer sa deviznog na dinarski racun");
        System.out.println("c) Provera stanja");
        System.out.println("d) Kraj");
    }

    private static float procitajPozitivanIznos(Scanner scanner, String poruka) {
        while (true) {
            System.out.println(poruka);
            // Dozvoljavamo i zarez i tacku kao decimalni separator.
            String unos = scanner.nextLine().trim().replace(',', '.');

            try {
                float iznos = Float.parseFloat(unos);
                if (iznos > 0) {
                    return iznos;
                }

                System.out.println("Unesite pozitivan iznos.");
            } catch (NumberFormatException e) {
                System.out.println("Unesite broj.");
            }
        }
    }

    private static String formatiraj(float vrednost) {
        // DecimalFormat nije thread-safe, pa je sinhronizacija najjednostavnija
        // zastita iako ovde realno radimo u jednom thread-u.
        synchronized (FORMAT) {
            return FORMAT.format(vrednost);
        }
    }
}
