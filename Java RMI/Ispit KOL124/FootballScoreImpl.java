import java.rmi.RemoteException;
import java.rmi.server.UnicastRemoteObject;
import java.util.LinkedHashMap;
import java.util.Map;

public class FootballScoreImpl extends UnicastRemoteObject implements FootballScore {
    private final Map<Integer, Match> utakmice = new LinkedHashMap<>();

    public FootballScoreImpl() throws RemoteException {
        super();
        utakmice.put(1, new MatchImpl(1, "Radnicki Nis", "Partizan", new Stadium("Cair", "Nis")));
        utakmice.put(2, new MatchImpl(2, "Crvena zvezda", "Vojvodina", new Stadium("Rajko Mitic", "Beograd")));
    }

    public synchronized String getAllResults() throws RemoteException {
        StringBuilder sb = new StringBuilder();

        for (Match match : utakmice.values()) {
            sb.append(match.getResult()).append(System.lineSeparator());
        }

        return sb.toString();
    }

    public synchronized Match getMatch(int id) throws RemoteException {
        return utakmice.get(id);
    }
}
