#pragma once
#include <string>

class ClassExample
{
	public:
		ClassExample(std::string message);
		~ClassExample() {};
		void PrintMessage();
	private:
		std::string message;
};