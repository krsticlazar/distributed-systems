import java.rmi.*;

public interface Generator extends Remote {
    public void generisiProsteBrojeve(int n, int m, Callback cb) throws RemoteException;
}
