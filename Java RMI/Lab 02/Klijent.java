import java.rmi.Naming;
import java.util.Scanner;

/**
 * Klijentska konzolna aplikacija za rad sa mobilnim operaterom.
 *
 * Klijent prikazuje meni, trazi broj telefona i poziva odgovarajuce metode na
 * udaljenim objektima.
 */
public class Klijent {
    public static void main(String[] args) {
        try (Scanner scanner = new Scanner(System.in)) {
            // Prvo preuzimamo glavni udaljeni objekat aplikacije.
            Operater operater = (Operater) Naming.lookup("rmi://localhost:1099/Operater");

            while (true) {
                ispisiMeni();
                String opcija = scanner.nextLine().trim().toLowerCase();

                // Opcija "e" zavrsava rad klijenta.
                if (opcija.equals("e")) {
                    break;
                }

                // Sve ostalo van definisanog menija odbacujemo.
                if (!opcija.equals("a") && !opcija.equals("b") && !opcija.equals("c") && !opcija.equals("d")) {
                    System.out.println("Nepoznata opcija.");
                    System.out.println();
                    continue;
                }

                if (opcija.equals("a")) {
                    System.out.println("Izabrali ste opciju za uplatu dodatnih minuta:");
                } else if (opcija.equals("b")) {
                    System.out.println("Izabrali ste opciju za uplatu dodatnih poruka:");
                } else if (opcija.equals("c")) {
                    System.out.println("Izabrali ste opciju za uplatu dodatnog interneta:");
                } else {
                    System.out.println("Izabrali ste opciju za proveru stanja:");
                }

                System.out.println("Unesite broj telefona korisnika:");
                String broj = scanner.nextLine().trim();
                // Operater vraca udaljeni objekat koji predstavlja konkretnog korisnika.
                Korisnik korisnik = operater.vratiKorisnika(broj);

                if (korisnik == null) {
                    System.out.println("Korisnik sa zadatim brojem ne postoji.");
                    System.out.println();
                    continue;
                }

                if (opcija.equals("a")) {
                    int minuti = procitajPozitivanBroj(scanner, "Unesite broj dodatnih minuta:");
                    // Poslovna logika izvrsava se na serveru, ne na klijentu.
                    korisnik.uplatiMinute(minuti);
                    System.out.println("Minuti su uspesno uplaceni.");
                    System.out.println();
                } else if (opcija.equals("b")) {
                    int poruke = procitajPozitivanBroj(scanner, "Unesite broj dodatnih poruka:");
                    korisnik.uplatiPoruke(poruke);
                    System.out.println("Poruke su uspesno uplacene.");
                    System.out.println();
                } else if (opcija.equals("c")) {
                    int internet = procitajPozitivanBroj(scanner, "Unesite broj dodatnih megabajta:");
                    korisnik.uplatiInternet(internet);
                    System.out.println("Internet je uspesno uplacen.");
                    System.out.println();
                } else {
                    // Za proveru stanja server vraca serijalizovan snapshot.
                    Stanje stanje = korisnik.vratiStanje();
                    ispisiStanje(stanje);
                }
            }
        } catch (Exception e) {
            e.printStackTrace();
        }
    }

    private static void ispisiMeni() {
        // Meni je odvojen u posebnu metodu da bi main ostao pregledan.
        System.out.println("Dobrodosli u korisnicki servis mobilnog operatera. Za nastavak izaberite opciju:");
        System.out.println("a) Uplata Minuta");
        System.out.println("b) Uplata Poruka");
        System.out.println("c) Uplata Interneta");
        System.out.println("d) Provera stanja");
        System.out.println("e) Kraj");
    }

    private static int procitajPozitivanBroj(Scanner scanner, String poruka) {
        while (true) {
            System.out.println(poruka);
            String unos = scanner.nextLine().trim();

            try {
                int vrednost = Integer.parseInt(unos);
                if (vrednost > 0) {
                    return vrednost;
                }

                // Negativni brojevi i nula nemaju smisla za dopunu.
                System.out.println("Unesite pozitivan ceo broj.");
            } catch (NumberFormatException e) {
                System.out.println("Unesite ceo broj.");
            }
        }
    }

    private static void ispisiStanje(Stanje stanje) {
        // Ova metoda samo formatira ispis; ne sadrzi poslovnu logiku.
        System.out.println("Trenutno stanje korisnika:");
        System.out.println("Minuti: " + stanje.vratiMinute());
        System.out.println("Poruke: " + stanje.vratiPoruke());
        System.out.println("Internet: " + stanje.vratiInternet() + " MB");
        System.out.println("Racun: " + stanje.vratiRacun() + " din");
        System.out.println();
    }
}
