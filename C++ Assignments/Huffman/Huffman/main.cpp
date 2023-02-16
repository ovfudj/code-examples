#include <iostream>
#include <queue>
#include <unordered_map>
#include <bitset>
#include <fstream>
#include <sstream>
using namespace std;

struct Node 
{
    char data;
    int frequency;
    Node* left, * right;
};

struct Compare 
{
    bool operator()(Node* left, Node* right) 
    {
        return (left->frequency > right->frequency);
    }
};

void appendPath(struct Node* root, string str, unordered_map<char, string>& bytePaths) 
{
    if (!root)
        return;
    if (root->data != '$')
        bytePaths[root->data] = str;
    appendPath(root->left, str + "0", bytePaths);
    appendPath(root->right, str + "1", bytePaths);
}

Node* getHuffmanTree(string text)
{
    unordered_map<char, int> freq;
    for (char c : text)
        freq[c]++;

    priority_queue<Node*, vector<Node*>, Compare> nodes;
    for (auto pair : freq)
    {
        Node* node = new Node;
        node->data = pair.first;
        node->frequency = pair.second;
        node->left = nullptr;
        node->right = nullptr;
        nodes.push(node);
    }

    while (nodes.size() > 1)
    {
        Node* left = nodes.top();
        nodes.pop();
        Node* right = nodes.top();
        nodes.pop();

        Node* node = new Node;
        node->data = '$';
        node->frequency = left->frequency + right->frequency;
        node->left = left;
        node->right = right;
        nodes.push(node);
    }

    Node* root = nodes.top();

    return root;
}

unordered_map<char, string> getBytePaths(string text, Node* root) 
{
    unordered_map<char, string> bytePath;
    appendPath(root, "", bytePath);

    return bytePath;
}

string getBitstring(const string& input, const unordered_map<char, string>& bytePaths)
{
    //Create a character representation of the 1 and 0s of the result file
    //the 1 and 0s will be interpreted to the smaller byte version in a separate method
    string bits;

    for (char c : input)
    {
        bits += bytePaths.at(c);
    }

    int padding = 8 - (bits.length() % 8);
    if (padding < 8)
    {
        bits += string(padding, '0');
    }

    return bits;
}

vector<char> getBitstringAsBytes(const string& bitstring)
{
    vector<char> bytes;

    for (int i = 0; i < bitstring.length(); i += 8)
    {
        bitset<8> bits(bitstring.substr(i, 8));
        bytes.push_back(bits.to_ulong());
    }

    return bytes;
}

int main() 
{
    //Read the file to a string
    ifstream inputFile("resource.txt");
    stringstream textStream;
    textStream << inputFile.rdbuf();
    string text = textStream.str();

    //Make the tree
    Node* root = getHuffmanTree(text);

    //The path of each byte to their node in the tree.
    unordered_map<char, string> bytePaths = getBytePaths(text,root);

    cout << "Huffman Codes:" << endl;

    for (auto pair : bytePaths) 
    {
        cout << pair.first << " : " << pair.second << endl;
    }

    string bitstring = getBitstring(text, bytePaths);
    vector<char> bytes = getBitstringAsBytes(bitstring);

    ofstream outputFile("compressed.txt", ios::out | ios::binary);
    for (char b : bytes)
    {
        outputFile.write((char*)&b, sizeof(b));
    }
    outputFile.close();
    
    ofstream oFile("decompressed.txt", ios::out | ios::binary);
    ifstream iFile("compressed.txt", ios::in | ios::binary);
    Node* currentNode = root;
    char byte;

    while (iFile.get(byte))
    {
        for (int i = 7; i >= 0; i--)
        {
            bool bit = (byte >> i) & 1;
            if (bit) {
                currentNode = currentNode->right;
            }
            else
            {
                currentNode = currentNode->left;
            }

            if(currentNode -> data != '$')
            { 
                oFile.put(currentNode->data);
                currentNode = root;
            }
        }
    }
    iFile.close();
    oFile.close();

    return 0;
}