#include "ClassExample.h"
#include <iostream>

ClassExample::ClassExample(std::string message)
{
	this->message = message;
}

void ClassExample::PrintMessage()
{
	std::cout << message;
}