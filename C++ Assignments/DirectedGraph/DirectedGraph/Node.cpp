#include "Node.h"
#include <list>
#include <string>


Node::Node()
{
}

Node::Node(std::list<std::string> childrenLabels)
{
	this->childrenLabels = childrenLabels;
}
