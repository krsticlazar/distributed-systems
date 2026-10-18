import java.rmi.Naming;
import java.util.Scanner;

public class Klijent {
    public static void main(String[] args) {
        try {
            EAukcija aukcija = (EAukcija) Naming.lookup("rmi://localhost/EAukcija");   // Dobijamo stub glavnog servisa.
            Scanner scanner = new Scanner(System.in);                                   // Konzolni unos korisnika.

            System.out.println("Dobrodosli na elektronsku aukciju. Za nastavak unesite vase licne podatke:");
            System.out.println("Identifikator:");
            System.out.print("/>");
            String klijentId = scanner.nextLine().trim();

            System.out.println("Ime:");
            System.out.print("/>");
            String ime = scanner.nextLine().trim();

            System.out.println("Prezime:");
            System.out.print("/>");
            String prezime = scanner.nextLine().trim();

            KlijentAukcije klijentAukcije = new KlijentAukcije(klijentId, ime, prezime);   // Lokalni serializable objekat klijenta.

            while (true) {                                                               // Jedan klijent moze da obradi vise eksponata redom.
                System.out.println();
                System.out.println("Unesite identifikator za eksponat od interesa ili 'kraj' za izlaz:");
                System.out.print("/>");
                String idEksponata = scanner.nextLine().trim();

                if ("kraj".equalsIgnoreCase(idEksponata)) {                              // Prekid rada klijenta.
                    break;
                }

                Eksponat eksponat = aukcija.vratiEksponat(idEksponata);                  // Glavni servis vraca trazeni remote eksponat.
                if (eksponat == null) {                                                  // Uneti ID ne postoji u bazi aukcije.
                    System.out.println("Eksponat sa zadatim identifikatorom ne postoji.");
                    continue;
                }

                System.out.println("Naziv eksponata je:");
                System.out.println(eksponat.vratiNaziv());
                System.out.println("Cena eksponata je:");
                System.out.println(eksponat.vratiCenu());

                System.out.println("Izaberite opciju:");
                System.out.println("a) Licitacija");
                System.out.println("b) Odustajanje");
                System.out.println("c) Sledeci eksponat");
                System.out.print("/>");
                String opcija = scanner.nextLine().trim();

                if ("a".equalsIgnoreCase(opcija)) {                                      // Licitacija nad izabranim eksponatom.
                    System.out.println("Za koliko uvecavate iznos eksponata?");
                    System.out.print("/>");
                    int iznos = Integer.parseInt(scanner.nextLine().trim());              

                    eksponat.povecajCenu(iznos);                                         
                    eksponat.prijaviLicitaciju(klijentAukcije);                          // Server pamti ko trenutno licitira.

                    System.out.println("Nova cena eksponata je:");
                    System.out.println(eksponat.vratiCenu());
                } else if ("b".equalsIgnoreCase(opcija)) {                               
                    eksponat.odustaniOdLicitacije(klijentAukcije.vratiKlijentAukcijeId());   // Brise klijenta ako je on trenutno prijavljen.
                    System.out.println("Odustajanje od licitacije je evidentirano.");
                }

                KlijentAukcije aktivniKlijent = eksponat.vratiKlijentaAukcije();         // Provera trenutnog stanja na serveru.
                if (aktivniKlijent == null) {
                    System.out.println("Za ovaj eksponat trenutno nema prijavljenog klijenta.");
                } else {
                    System.out.println("Trenutno prijavljeni klijent je:");
                    System.out.println(aktivniKlijent);
                }
            }

            scanner.close();                                                              // Zatvaranje skenera pre izlaza.
        } catch (Exception e) {
            e.printStackTrace();
        }
    }
}
