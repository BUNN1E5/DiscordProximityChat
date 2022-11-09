using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace DiscordProximityChat{
    public class Utils{
        //sq = x^2, yes pass in an already squared x
        public static byte CalculateProximityVolume(float sqx){
            //Function is 1/(x^2 + 1)
            return (byte)(128 / (float) ((sqx) + 1));
        }
    }

    public class BidirectionalDictionary<T1, T2> : IEnumerable<KeyValuePair<T1, T2>>{
        private Dictionary<T1, T2> forward = new();
        private Dictionary<T2, T1> reverse = new();

        public void Add(T1 A, T2 B){
            forward.Add(A, B);
            reverse.Add(B, A);
        }

        public bool Remove(T1 A, T2 B){
            return forward.Remove(A) & reverse.Remove(B);
        }
        
        public bool Remove(T2 key){
            return forward.Remove(reverse[key]) & reverse.Remove(key);
        }
        
        public bool Remove(T1 key){
            return reverse.Remove(forward[key]) & forward.Remove(key);
        }

        public T1 this[T2 key]{
            get{ return reverse[key]; }
            set{ reverse[key] = value; }
        }
        
        public T2 this[T1 key]{
            get{ return forward[key]; }
            set{ forward[key] = value; }
        }

        public void TryGetValue(T1 key, out T2 o){
            o = forward[key];
        }
        
        public void TryGetValue(T2 key, out T1 o){
            o = reverse[key];
        }

        public bool ContainsKey(T1 key){
            return forward.ContainsKey(key);
        }
        
        public bool ContainsKey(T2 key){
            return reverse.ContainsKey(key);
        }

        IEnumerator<KeyValuePair<T1, T2>> IEnumerable<KeyValuePair<T1, T2>>.GetEnumerator(){
            // ReSharper disable once HeapView.BoxingAllocation
            return forward.GetEnumerator();
        }

        public IEnumerator GetEnumerator(){
            // ReSharper disable once HeapView.BoxingAllocation
            return forward.GetEnumerator();
        }
    }
}