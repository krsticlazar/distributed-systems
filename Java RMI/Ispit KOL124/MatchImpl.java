import java.rmi.RemoteException;
import java.rmi.server.UnicastRemoteObject;
import java.util.ArrayList;
import java.util.List;

public class MatchImpl extends UnicastRemoteObject implements Match {
    private final int id;
    private final String homeTeam;
    private final String awayTeam;
    private final Stadium stadium;
    private final List<Callback> callbacks = new ArrayList<>();
    private int homeGoals;
    private int awayGoals;

    public MatchImpl(int id, String homeTeam, String awayTeam, Stadium stadium) throws RemoteException {
        super();
        this.id = id;
        this.homeTeam = homeTeam;
        this.awayTeam = awayTeam;
        this.stadium = stadium;
    }

    public void addHomeGoal() throws RemoteException {
        List<Callback> pretplaceni;

        synchronized (this) {
            homeGoals++;
            pretplaceni = new ArrayList<>(callbacks);
        }

        obavestiPretplacene(pretplaceni);
    }

    public void addAwayGoal() throws RemoteException {
        List<Callback> pretplaceni;

        synchronized (this) {
            awayGoals++;
            pretplaceni = new ArrayList<>(callbacks);
        }

        obavestiPretplacene(pretplaceni);
    }

    public synchronized Stadium getStadium() throws RemoteException {
        return stadium;
    }

    public synchronized String getResult() throws RemoteException {
        return id + ": " + homeTeam + " " + homeGoals + " - " + awayGoals + " " + awayTeam;
    }

    public synchronized void subscribe(Callback callback) throws RemoteException {
        callbacks.add(callback);
        System.out.println("Klijent je pretplacen na utakmicu " + id);
    }

    private void obavestiPretplacene(List<Callback> pretplaceni) {
        List<Callback> neaktivni = new ArrayList<>();

        for (Callback callback : pretplaceni) {
            try {
                callback.resultChanged(id);
            } catch (RemoteException e) {
                neaktivni.add(callback);
            }
        }

        synchronized (this) {
            callbacks.removeAll(neaktivni);
        }
    }
}
