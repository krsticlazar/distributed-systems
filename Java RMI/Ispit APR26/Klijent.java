import java.rmi.*;
import java.rmi.server.*;
import java.util.*;

public class Klijent {
    private Generator generator;

    public Klijent() {
        try {
            generator = (Generator) Naming.lookup("rmi://localhost:1099/Generator");
        } catch (Exception e) {
        }
    }

    public void pokreni() {
        Scanner scanner = new Scanner(System.in);
        CallbackImpl cb = null;

        try {
            System.out.println("Unesite N:");
            int n = Integer.parseInt(scanner.nextLine());
            System.out.println("Unesite M:");
            int m = Integer.parseInt(scanner.nextLine());

            cb = new CallbackImpl();
            generator.generisiProsteBrojeve(n, m, cb);
        } catch (Exception e) {
        } finally {
            if (cb != null) {
                try {
                    UnicastRemoteObject.unexportObject(cb, true);
                } catch (Exception e) {
                }
            }
        }

        scanner.close();
    }

    public static void main(String[] args) {
        new Klijent().pokreni();
    }

    public class CallbackImpl extends UnicastRemoteObject implements Callback {
        public CallbackImpl() throws RemoteException {
            super();
        }

        public void prostBrojGenerisan(int broj) throws RemoteException {
            System.out.println("Prost broj: " + broj);
        }
    }
}
