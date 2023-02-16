#pragma once
#include <string>
#include <list>
class Node
{
	public:
		Node();
		Node(std::list<std::string> labels);
		std::list<std::string> childrenLabels;
};

