#event properties (no comments/etc. here are saved)
parent_index = -1;
uses_physics = false;
max_clients:int<0, 80> = 20;

#event create
global.IS_CLIENT = false;

port = 90955;
server_id_count = 0;
socket_list = ds_list_create();
network = network_create_server(network_socket_tcp,port,max_clients);

draw_enable_drawevent(false);

#event destroy
buffer_delete(server_buffer);

#event async_network
type_event = ds_map_find_value(async_load,"type");
switch(type_event) {
	case network_type_connect:
		socket = ds_map_find_value(async_load,"socket");
		ds_list_add(socket_list,socket);
		signal_network_event("AssignClientID",{client_id : socket},socket);
		execute_network_event("OnConnect",{client_id : socket});
		signal_network_event_all("OnConnect",{client_id : socket});
		break;
		
	case network_type_disconnect:
		socket = ds_map_find_value(async_load,"socket");
		ds_list_delete(socket_list,ds_list_find_index(socket_list,socket));
		execute_network_event("OnDisconnect",{client_id : socket});
		signal_network_event_all("OnDisconnect",{client_id : socket});
		break;
		
	case network_type_data:
		var buffer = ds_map_find_value(async_load,"buffer");
		socket = ds_map_find_value(async_load,"id");
		var buffer = ds_map_find_value(async_load,"buffer");
		buffer_seek(buffer,buffer_seek_start,0);
		var net_event_header = buffer_read(buffer,buffer_string);
		var parse = buffer_read(buffer,buffer_bool);
		while(parse) {
			var data_struct = SnapBufferReadBinary(buffer);
			execute_network_event(net_event_header,data_struct,socket);
			parse = buffer_read(buffer,buffer_bool);
		}
		buffer_delete(buffer);
		break;
}

