import java.rmi.Naming;
import java.util.Scanner;

public class Klijent {
    public static void main(String[] args) {
        String serverHost = args.length > 0 ? args[0] : "localhost";

        try (Scanner scanner = new Scanner(System.in)) {
            Kviz kviz = (Kviz) Naming.lookup("rmi://" + serverHost + ":1099/Kviz");
            kviz.pocetak();

            for (int i = 1; i <= 3; i++) {
                Pitanje p = kviz.vratiPitanje();
                if (p == null) {
                    break;
                }

                System.out.println("Pitanje " + i);
                System.out.println(p.vratiTekst());
                System.out.println();
                System.out.println("Unesite odgovor:");

                String odg = scanner.nextLine().trim();
                kviz.odgovori(odg);
                System.out.println();
            }

            System.out.println("Broj Poena:");
            System.out.println(kviz.vratiBrojPoena());
        } catch (Exception e) {
            e.printStackTrace();
        }
    }
}
