import java.rmi.*;
import java.rmi.server.*;

public class GeneratorImpl extends UnicastRemoteObject implements Generator {
    public GeneratorImpl() throws RemoteException {
        super();
    }

    public void generisiProsteBrojeve(int n, int m, Callback cb) throws RemoteException {
        for (int i = n; i <= m; i++) {
            if (prost(i)) {
                cb.prostBrojGenerisan(i);
            }
        }
    }

    private boolean prost(int broj) {
        if (broj < 2) {
            return false;
        }

        for (int i = 2; i * i <= broj; i++) {
            if (broj % i == 0) {
                return false;
            }
        }

        return true;
    }
}
