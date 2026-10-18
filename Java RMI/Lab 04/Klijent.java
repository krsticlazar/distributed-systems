import java.rmi.*;
import java.util.*;

public class Klijent {
    public static void main(String[] args) {
        Scanner scanner = new Scanner(System.in);

        try {
            EStudSluzba sluzba = (EStudSluzba) Naming.lookup("rmi://localhost:1099/EStudSluzba");

            while (true) {
                System.out.println("Dobrodosli u korisnicki servis studentske sluzbe. Za nastavak izaberite opciju:");
                System.out.println("a) Prijava ispita");
                System.out.println("b) Provera prijavljenih ispita");
                System.out.println("c) Kraj");

                String opcija = scanner.nextLine();
                if (opcija.equals("c")) {
                    break;
                }

                System.out.println("Unesite broj indeksa:");
                String brIndeksa = scanner.nextLine();
                Student student = sluzba.vratiStudenta(brIndeksa);

                if (student == null) {
                    System.out.println("Student ne postoji.");
                    continue;
                }

                if (opcija.equals("a")) {
                    System.out.println("Unesite naziv ispita:");
                    String ispit = scanner.nextLine();
                    student.prijaviIspit(ispit);
                } else if (opcija.equals("b")) {
                    Prijava prijava = student.vratiPrijavu();
                    System.out.println("Prijavljeni ispiti su:");
                    System.out.println(prijava.vratiIspite());
                }
            }
        } catch (Exception e) {
        }

        scanner.close();
    }
}
