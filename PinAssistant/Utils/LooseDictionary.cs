using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace WxAxW.PinAssistant.Utils
{
    [Serializable]
    public class LooseDictionary<TValue>
    {
        public class TraverseDetails
        {
            public string key;
            public TValue value;
            public TrieNode nodeResult;
            public string actualKey;
            public bool conflicting;
            public string blackListedWord;
            public bool deleteNode;
            public bool exactMatchOnly;

            public TValue conflictingNodeValue;
            public bool endOfDelete = false;

            public TraverseDetails(string key, TValue value = default, string actualKey = "", string blackListedWord = "", bool deleteNode = false, bool exactMatch = false)
            {
                this.key = key;
                this.value = value;
                this.actualKey = actualKey;
                this.blackListedWord = blackListedWord;
                this.deleteNode = deleteNode;
                this.exactMatchOnly = exactMatch;
            }
        }

        [JsonProperty("Version")]
        private readonly string m_version = "1.0";

        [JsonProperty("Alternate Dictionary")]
        public readonly Dictionary<string, TrieNode> altDictionary = new Dictionary<string, TrieNode>(); // dictionary to easily find/count/delete words instead of traversing trie dictionary

        [JsonProperty("Trie Node Dictionary")]
        private readonly TrieNode root = new TrieNode();

        public LooseDictionary()
        { }

        public bool Add(string key, TValue value, out bool conflicting, string blackListWords = "", bool exactMatchOnly = false)
        {
            key = key.ToLower();
            blackListWords = blackListWords.ToLower();
            conflicting = false;
            if (altDictionary.ContainsKey(key)) return false; // skip
            TraverseDetails traverseDetails = new TraverseDetails(key, value: value, blackListedWord: blackListWords, exactMatch: exactMatchOnly);
            root.Add(traverseDetails);
            altDictionary.Add(key, traverseDetails.nodeResult);
            conflicting = traverseDetails.conflicting;
            return true;
        }

        public bool Modify(string key, TValue newValue, bool newNodeExact, string newBlackListWords, bool exactMatch = false)
        {
            key = key.ToLower();
            newBlackListWords = newBlackListWords.ToLower();
            if (exactMatch)
            {
                if (!altDictionary.TryGetValue(key, out TrieNode nodeResult)) return false;
                nodeResult.NodeExactMatchOnly = newNodeExact;
                nodeResult.BlackListWords = newBlackListWords;
                if (nodeResult.Value.Equals(newValue)) return true; // do not modify if they're the same
                nodeResult.Value = newValue;
            }
            else if (!root.Modify(key, newValue, newNodeExact, newBlackListWords)) return false;
            return true;
        }

        public bool Remove(string key, bool exactMatch = false)
        {
            key = key.ToLower();
            if (exactMatch)
            {
                if (!altDictionary.Remove(key)) return false;
                root.TryDeleteLoose(key, out _, true, exactMatch);
            }
            else
            {
                if (!root.TryDeleteLoose(key, out string actualKey, true, exactMatch)) return false;
                altDictionary.Remove(actualKey);
            }
            return true;
        }

        public bool TryGetValueLoose(string key, out TValue value, bool exactMatch = false)
        {
            key = key.ToLower();
            value = default;

            if (altDictionary.TryGetValue(key, out TrieNode nodeResult))
            {
                value = nodeResult.Value;
                return true;
            }
            if (exactMatch) return false;
            return root.TryGetValueLoose(key, out value);
        }

        public bool TryGetValueLooseLite(string key, out TValue result)
        {
            key = key.ToLower();

            if (altDictionary.TryGetValue(key, out TrieNode nodeResult))
            {
                result = nodeResult.Value;
                return true;
            }
            return root.TryGetValueLooseRecursiveLite(key, out result);
        }

        public class TrieNode
        {
            [JsonProperty("Children")]
            private readonly Dictionary<char, TrieNode> m_children = new Dictionary<char, TrieNode>();

            private TValue m_value;                       // root is always null
            private bool m_nodeExactMatchOnly;    // use to determine if this node can only be accessed through exact searching
            private string m_blackListWord;         // blacklisted words, if this node was found, use this variable to compare with key, if key contains blacklist, ignore this node

            public TValue Value { get => m_value; set => m_value = value; }
            public bool NodeExactMatchOnly { get => m_nodeExactMatchOnly; set => m_nodeExactMatchOnly = value; }
            public string BlackListWords { get => m_blackListWord; set => m_blackListWord = value; }

            //private string key;     // used to determine the entire key string
            public TrieNode()
            { }

            public bool Add(TraverseDetails traverseDetails)
            {
                // before adding check dictionary if there's a loose search conflict
                if (!traverseDetails.exactMatchOnly) Traverse(traverseDetails); // autofill conflict with this method
                TrieNode currNode = this;
                for (int c = 0; c < traverseDetails.key.Length; c++)
                {
                    if (!currNode.m_children.TryGetValue(traverseDetails.key[c], out TrieNode child))
                    {
                        child = new TrieNode();
                        currNode.m_children.Add(traverseDetails.key[c], child);
                    }
                    currNode = child;
                }
                if (currNode.m_value != null) return false;
                traverseDetails.nodeResult = currNode;
                currNode.m_value = traverseDetails.value;
                currNode.m_nodeExactMatchOnly = traverseDetails.exactMatchOnly;
                currNode.m_blackListWord = traverseDetails.blackListedWord;

                return true;
            }

            public bool Modify(string key, TValue newValue, bool newNodeExact, string newBlackList)
            {
                if (!TryGetNodeLoose(key, out TrieNode nodeResult)) return false;
                nodeResult.m_nodeExactMatchOnly = newNodeExact;
                nodeResult.m_value = newValue;
                nodeResult.m_blackListWord = newBlackList;
                return true;
            }

            // delete this one node only, the others will be deleted from the recursive method
            public void Remove(char childNodeKey)
            {
                TrieNode childNodeToDelete = m_children[childNodeKey];
                // if this node has children do not cut the node off the main dictionary, only delete the instance
                if (childNodeToDelete.m_children.Count != 0)
                {
                    childNodeToDelete.Value = default;
                    return;
                }
                // delete node from main dictionary keep deleting until the parent's children is more than 1,
                /* ex. delete key foob
                 *     f - don't remove f cause 'u' child exists
                 *    u o - remove child o from f parent
                 *       o - remove child o from o parent
                 *        b - remove child b from o parent
                 *     f - don't remove f cause node has result
                 *      o - remove child o from f parent
                 *       o - remove child o from o parent
                 *        b - remove child b from o parent
                 */
                m_children.Remove(childNodeKey); // remove child from parent
            }

            // overload for traverse delete
            public bool TryDeleteLoose(string key, out string actualKey, bool deleteKeyNode, bool exactMatch)
            {
                TraverseDetails traverseDetails = new TraverseDetails(key, deleteNode: deleteKeyNode, exactMatch: exactMatch);
                bool found = Traverse(traverseDetails);
                actualKey = traverseDetails.actualKey;
                return found;
            }

            public bool TryGetNodeLoose(string key, out TrieNode nodeResult)
            {
                TraverseDetails traverseDetails = new TraverseDetails(key);
                bool found = Traverse(traverseDetails);
                nodeResult = traverseDetails.nodeResult;
                return found;
            }

            public bool TryGetValueLoose(string key, out TValue result)
            {
                TraverseDetails traverseDetails = new TraverseDetails(key);
                bool found = Traverse(traverseDetails);
                result = traverseDetails.value;
                return found;
            }

            public bool TryGetValueLoose(string key, out TrieNode nodeResult, out string actualKey)
            {
                TraverseDetails traverseDetails = new TraverseDetails(key);
                bool found = Traverse(traverseDetails);
                nodeResult = traverseDetails.nodeResult;
                actualKey = traverseDetails.actualKey;
                return found;
            }

            public bool Traverse(TraverseDetails traverseDetails)
            {
                return TraverseRecursive(0, new StringBuilder(), traverseDetails);
            }

            // finds the key either loosely or exact match, ex: entryKey = oobar, searchKey = barf*oobar*foo
            private bool TraverseRecursive(int currentIndex, StringBuilder keyBuilder, TraverseDetails td)
            {
                // keep checking until end of key, if true means there's more chars to check
                if (currentIndex >= td.key.Length) return false;

                // to check if the child entered gave no result, therefore get out of the child and remove a prefix to keep looping
                // ex. entry = runestone | copper, search = rock4_copper, entered r child, but returned false all the way, but with this bool, it will keep continuing
                bool enteredChild = true;

                char currentChar = td.key[currentIndex];
                keyBuilder.Append(currentChar); // add char to keybuilder (to build the actual key that's found)
                // if currentNode's children does not have the specified char key, exit this method to backtrack and check the next char
                // ex. entry in current node: rock , r doesn't exist, check o next time.
                // rock, ock, ck, k
                if (!m_children.TryGetValue(currentChar, out TrieNode nodeToCheck))
                {
                    if (td.exactMatchOnly) return false;
                    nodeToCheck = this;
                    keyBuilder.Clear(); // clear keybuilder because current path failed
                    enteredChild = false; // set to false to not run this method again in this current TrieNode class
                }

                // if node exists, check if node is valid ie. node has result is node can only be found through exact match, or key search does not contain blacklisted words by node

                if (IsNodeValid(nodeToCheck, td.key, currentIndex, td.exactMatchOnly))
                {
                    // node is valid, return results
                    td.value = nodeToCheck.m_value;
                    td.nodeResult = nodeToCheck;
                    td.actualKey = keyBuilder.ToString();
                    td.conflicting = CheckChildrenValid(td.key, td.exactMatchOnly);
                    if (td.deleteNode) Remove(currentChar);
                    return true;
                }
                bool foundValid = false;
                for (int i = 0; i < 2; i++)
                {
                    // invalid node result therefore keep looping until end of key or end of node (either the node can only be found through exact searching only or there's a blacklisted word that the node doens't want)
                    foundValid = nodeToCheck.TraverseRecursive(currentIndex + 1, keyBuilder, td);

                    // if this, traverse inside a child, get out and traverse in itself but with a different char index.
                    if (foundValid) break; // if found a valid match, break out of loop to do delete check
                    if (!enteredChild) break; // else if this, did not traverse inside a child (traversed itself) return false entirely
                    // if (td.exactMatchOnly) return false; // not necessary because it's impossible to reach this point when exactmatch is true
                    // else this, entered a child, therefore exit child and traverse itself but increment currentindex to check the next letter after that
                    nodeToCheck = this;
                    keyBuilder.Clear(); // clear keybuilder because current path
                }
                if (!foundValid) return false;
                // found result
                // if in delete mode, run remove child method, if reached a node with result, end of deletion
                if (!td.endOfDelete && td.deleteNode && m_children[currentChar].Value == null) Remove(currentChar);
                return true;
            }

            private bool IsNodeValid(TrieNode nodeToCheck, string key, int index, bool exactMatchOnly = false)
            {
                if (nodeToCheck.m_value == null || // check if node is filled
                    ((nodeToCheck.m_nodeExactMatchOnly || exactMatchOnly) && index != key.Length - 1) || // found a close match, but can only be accessed through exact match
                    (!string.IsNullOrEmpty(nodeToCheck.m_blackListWord) && key.IndexOf(nodeToCheck.BlackListWords) != -1) // if the found node has a blacklisted word that is not found in the main key
                   )
                {
                    return false;
                }
                return true;
            }

            private void FindAllNodeValues(List<TrieNode> allValues)
            {
                // loop through node's childrend
                foreach (TrieNode node in m_children.Values)
                {
                    allValues.Add(node); // add current node
                    node.FindAllNodeValues(allValues); // get all children nodes of curr node
                }
            }

            private bool CheckChildrenValid(string key, bool exactMatch)
            {
                if (exactMatch) return false;
                List<TrieNode> descendants = new List<TrieNode>();
                FindAllNodeValues(descendants);
                for (int i = 0; i < descendants.Count; i++)
                {
                    TrieNode currChildNode = descendants[i];
                    if (currChildNode.m_value == null ||  // check if node is filled
                        currChildNode.m_nodeExactMatchOnly) continue; // found a close match, but can only be accessed through exact match
                    bool keyBlacklisted = !string.IsNullOrEmpty(currChildNode.m_blackListWord) && key.IndexOf(currChildNode.BlackListWords) != -1;
                    if (keyBlacklisted) continue;

                    return true;
                }
                return false;
            }

            // used solely for searching dictionary with loose key search
            public bool TryGetValueLooseRecursiveLite(string key, out TValue result, int currentIndex = 0)
            {
                result = default(TValue);
                // keep checking until end of key, if true means there's more chars to check
                if (currentIndex >= key.Length) return false;

                bool enteredChild = true;
                char currentChar = key[currentIndex];
                // if currentNode's children does not have the specified char key, exit this method to backtrack and check the next char
                // ex. entry in current node: rock , r doesn't exist, check o next time.
                // rock, ock, ck, k
                if (!m_children.TryGetValue(currentChar, out TrieNode nodeToCheck))
                {
                    nodeToCheck = this;
                    enteredChild = false;
                }

                // if node exists, check if node is valid ie. node has result is node can only be found through exact match, or key search does not contain blacklisted words by node

                if (IsNodeValid(nodeToCheck, key, currentIndex))
                {
                    // node is valid, return results
                    result = nodeToCheck.m_value;
                    return true;
                }

                bool foundValid = false;
                for (int i = 0; i < 2; i++)
                {
                    // invalid node result therefore keep looping until end of key or end of node (either the node can only be found through exact searching only or there's a blacklisted word that the node doens't want)
                    foundValid = nodeToCheck.TryGetValueLooseRecursiveLite(key, out result, currentIndex + 1);

                    // if this, traverse inside a child, get out and traverse in itself but with a different char index.
                    if (foundValid) break; // if found a valid match, break out of loop to do delete check
                    if (!enteredChild) break; // else if this, did not traverse inside a child (traversed itself) return false entirely
                    // if (td.exactMatchOnly) return false; // not necessary because it's impossible to reach this point when exactmatch is true
                    // else this, entered a child, therefore exit child and traverse itself but increment currentindex to check the next letter after that
                    nodeToCheck = this;
                }

                if (!foundValid) return false;
                return true;
            }

            /* old search method
            // finds the key either loosely or exact match, ex: entryKey = oobar, searchKey = barf*oobar*foo
            public bool TryGetValueLoose(string key, out TValue result, out TrieNode nodeResult, out string actualKey)
            {
                key = key.ToLower();
                TrieNode currNode = this;
                result = default(TValue);
                nodeResult = default(TrieNode);
                actualKey = default(string);
                StringBuilder keyBuilder = new StringBuilder(key);
                // 1. example dictionary entry key: "oobar"
                for (int i = 0; i < key.Length; i++)
                {
                    bool noEarlyBreak = true;
                    for (int j = i; j < key.Length; j++)
                    {
                        // 2. if exact search input: fooba, check word if found, check f, fail, if fail, set invalid stop the loop, check condition, reset validity and go to next char to search for a word starting with o, etc. etc. if loop ends properly, means valid, means there's a found match.
                        if (!currNode.m_children.TryGetValue(key[j], out TrieNode child))
                        { noEarlyBreak = false; break; }  // no match in this current prefix

                        // found node
                        currNode = child;

                        // analyze node for conditions that might not work
                        if (currNode.m_value == null || currNode.m_nodeExactMatchOnly // found a close match that's findable through loose search even though there's more char in the key
                            || key.IndexOf(currNode.BlackListWords) != -1) continue; // continue if the found node has a blacklisted word that is found in the main key
                        if (keyBuilder.Length > 1) keyBuilder.Remove(1, keyBuilder.Length - j - 1);
                        break;
                    }
                    /* old condition setup (UGLI)
                    if (exactMatch && !noEarlyBreak) return false; // 2. return false (end method) if exactMatch is true and it's not valid
                    else if (!noEarlyBreak && currNode.result == null) currNode = this; // 2. reset to root if found key is null then check the next chars (ie. o o b a)
                    else if (!noEarlyBreak && currNode.result != null && currNode.nodeExactMatchOnly) return false;
                    else if (noEarlyBreak && i > 0 && currNode.nodeExactMatchOnly) return false;
                    else if (noEarlyBreak) break; // 3. stop the loop if there's a valid loop match

                    // what to do if invalid search (prematurely ended current prefix key search)
                    if (!noEarlyBreak)
                    {
                        // removed cause alternate dictionary implemented to search for exact matches easily
                        //if (exactMatch) return false;       // end entirely because exact match only has 1 chance and that 1 chance ended early = no existing key

                        if (currNode == null                // since it's empty reset the current node to check the next char in the root node
                         || currNode.m_nodeExactMatchOnly)    // happens when loop ended up in a node with a result that can only be searched through exact matching ex. entry is foo set to exactMatchOnly. searched 'foob' loose(exactMatch = false), key 'b' doesn't exist so end point is at o, but foo can only be searched when it's exact,
                            currNode = this;                // therefore go to next loop and find oob ob b because search is set to loose
                    }
                    else // search is valid (completed the entire key search loop without encountering an empty node)
                    {
                        if (i > 0 && currNode.m_nodeExactMatchOnly)  // happens when you're not in the first iteration and you found a node that can only be found through exact matching
                            return false;
                        break;
                    }
                    keyBuilder.Remove(0, 1);
                }

                if (currNode.m_value == null) return false; // check if null, meaning invalid

                // 4 since the test search is fooba the key found should be null since there's no entry key named 'ooba', the search entry must have oobar, could be foobar f oobar foo, etc.
                result = currNode.m_value;
                nodeResult = currNode;
                actualKey = keyBuilder.ToString();

                return true;
            }
            */
        }
    }
}