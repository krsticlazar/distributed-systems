import java.rmi.*;
import java.rmi.server.*;

public class StudentImpl extends UnicastRemoteObject implements Student {
    private String brIndeksa;
    private PrijavaImpl prijava;

    public StudentImpl(String brIndeksa) throws RemoteException {
        super();
        this.brIndeksa = brIndeksa;
        prijava = new PrijavaImpl();
    }

    public Prijava vratiPrijavu() throws RemoteException {
        return prijava;
    }

    public void prijaviIspit(String ispit) throws RemoteException {
        prijava.dodajIspit(ispit);
    }
}
