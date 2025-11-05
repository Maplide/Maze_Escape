using UnityEngine;
using System.Collections.Generic;

public class LevelSpawner : MonoBehaviour {
  public DifficultyProfile profile;
  public GameObject plantPrefab, electricPrefab, rawPrefab, evaPrefab;
  public Transform plantSpawnsParent, electricSpawnsParent, rawSpawnsParent, evaSpawnsParent;

  List<Transform> P = new(), E = new(), R = new(), V = new();

  void Awake() {
    if (!profile) { Debug.LogError("Falta DifficultyProfile"); return; }
    Cache(plantSpawnsParent, P); Cache(electricSpawnsParent, E); Cache(rawSpawnsParent, R); Cache(evaSpawnsParent, V);

    int nP = Mathf.RoundToInt(profile.totalEnemies * profile.plantRatio);
    int nE = Mathf.RoundToInt(profile.totalEnemies * profile.electricRatio);
    int nR = Mathf.RoundToInt(profile.totalEnemies * profile.rawRatio);
    int nV = Mathf.RoundToInt(profile.totalEnemies * profile.evaRatio);
    int diff = profile.totalEnemies - (nP+nE+nR+nV);
    while (diff != 0) { if (diff>0) { nP++; diff--; } else if (nP>0){ nP--; diff++; } else break; }

    Spawn(plantPrefab, P, nP);
    Spawn(electricPrefab, E, nE);
    Spawn(rawPrefab, R, nR);
    Spawn(evaPrefab, V, nV);
  }

  void Cache(Transform parent, List<Transform> list){ list.Clear(); if(!parent) return; foreach(Transform t in parent) list.Add(t); if(list.Count==0) list.Add(parent); }
  void Spawn(GameObject prefab, List<Transform> pts, int count){
    if (!prefab || pts.Count==0 || count<=0) return;
    for(int i=0;i<count;i++){ var t=pts[i%pts.Count]; Instantiate(prefab, t.position, t.rotation, transform); }
  }
}
