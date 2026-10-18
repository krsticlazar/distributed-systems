import java.rmi.*;

public interface Student extends Remote {
    public Prijava vratiPrijavu() throws RemoteException;
    public void prijaviIspit(String ispit) throws RemoteException;
}
