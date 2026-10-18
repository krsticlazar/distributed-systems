import java.rmi.Remote;
import java.rmi.RemoteException;

public interface Eksponat extends Remote {                             
    void prijaviLicitaciju(KlijentAukcije ka) throws RemoteException;  

    KlijentAukcije vratiKlijentaAukcije() throws RemoteException;       

    void odustaniOdLicitacije(String klijentAukcijeId) throws RemoteException;   

    String vratiNaziv() throws RemoteException;                         

    int vratiCenu() throws RemoteException;                            

    void povecajCenu(int iznos) throws RemoteException;                
}
