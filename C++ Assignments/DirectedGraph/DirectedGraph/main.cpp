#include <iostream>
#include <fstream>
#include <iterator>
#include <unordered_map>
#include <sstream>
#include <list>

#include "Node.h"

using namespace std;
void split(string const& input, const char delim, list<string>& out)
{
	stringstream stream(input);

	string part;
	while (getline(stream, part, delim))
	{
		out.push_back(part);
	}
}

int main()
{
	unordered_map<string,unique_ptr<Node>> nodeMap;
	fstream file;
	file.open("resource.txt", ios::in);

	if (file.is_open())
	{
		string line;
		while (getline(file, line))
		{
			list<string> words;
			split(line, ' ', words);
			std::list<string>::iterator it = words.begin();
			string key = *it;
			list<string> children;
			++ it;
			for (; it != words.end(); ++ it)
			{
				children.push_front(*it);
			}
			nodeMap.insert_or_assign(key,make_unique<Node>(children));
		}
	}
	
	int command = 1;
	string input = "";
	string enabledString = " : [Enabled] ";
	string disabledString = " : [Disabled] ";

	while (command != -1)
	{
		switch (command)
		{
			case 0:
			//Receive Input
			cout << "a node_name                 : Add node \n" 
				 << "l node_name dependency_name : Link node to another \n" 
				 << "node_name                   : Deletes the labeled node \n" 
				 << "q                           : Quit \n";
			cin >> input;
			if (input.compare("a") == 0)
			{
				cin >> input;
				if (nodeMap.find(input) == nodeMap.end())
				{
					nodeMap.insert_or_assign(input, make_unique<Node>());
				}
				command = 1;
			}
			else if (input.compare("l") == 0)
			{
				cin >> input;
				if (nodeMap.find(input) != nodeMap.end())
				{
					Node* n = nodeMap.at(input).get();
					cin >> input;
					if (find(n->childrenLabels.begin(), n->childrenLabels.end(), input) == n -> childrenLabels.end())
					{
						n->childrenLabels.push_front(input);
					}
				}
				command = 1;
			}
			else if (input.compare("q") == 0)
			{
				command = -1;
			}
			else
			{
				if (nodeMap.find(input) != nodeMap.end())
				{
					nodeMap.erase(input);
				}
				command = 1;
			}

			system("cls");
			break;

			case 1:
			//Display Graph
			for (auto itr = nodeMap.begin(); itr != nodeMap.end(); itr++)
			{
				bool enabled = true;
				cout << itr->first << " -> ";
				for (auto cItr = itr->second->childrenLabels.begin(); cItr != itr->second->childrenLabels.end(); cItr++)
				{
					//If the node isn't in the map disable the node
					if (nodeMap.find(*cItr) == nodeMap.end())
					{
						enabled = false;
					}

					//Output the element
					cout << *cItr;
					if (cItr != -- itr->second->childrenLabels.end())
					{
						cout << ", ";
					}
				}

				if (enabled)
				{
					cout << enabledString;
				}
				else
				{
					cout << disabledString;
				}

				cout << endl;
			}
			cout << endl;
			command = 0;
			break;
		}
	}

	return 0;
}