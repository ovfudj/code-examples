#event properties (no comments/etc. here are saved)
parent_index = -1;
uses_physics = false;

#event create
time = 0;
network_events = ds_map_create();
network_client_data_callback_queue = ds_queue_create();
network_server_data_callback_map = ds_map_create();
network_server_data_callback_queue_all = ds_queue_create();

function __create_network_event(_header) {
	ds_map_add(network_events,_header,ds_list_create());	
}

function __connect_network_event(_header,_scope_object, _action) {
	if(!ds_map_exists(network_events,_header)) {
		__create_network_event(_header);	
	}
	var list = ds_map_find_value(network_events,_header);
	ds_list_add(list,[_scope_object,_action]);
}

function __signal_network_event(_header,_data,_socket_id) {
	if(global.IS_CLIENT) {
		ds_queue_enqueue(network_client_data_callback_queue,[_header,_data]);
	} else {
		if(is_undefined(ds_map_find_value(network_server_data_callback_map,_socket_id))) {
			ds_map_add(network_server_data_callback_map,_socket_id,ds_queue_create());
		}
		var network_server_data_callback_queue = ds_map_find_value(network_server_data_callback_map,_socket_id);
		ds_queue_enqueue(network_server_data_callback_queue,[_header,_data]);
	}
}

function __signal_network_event_all(_header,_data,_ignore_sockets) {
	ds_queue_enqueue(network_server_data_callback_queue_all,[_header,_data,_ignore_sockets]);
}

function __execute_network_event(_header,_net_data,_socket_id) {
	var net_event_list = ds_map_find_value(network_events,_header);
	if(!is_undefined(net_event_list)) {
		var length = ds_list_size(net_event_list);
		for(var i = 0; i < length; i ++) {
			var net_event_data = ds_list_find_value(net_event_list,i);
			with(net_event_data[0]) {
				net_event_data[1](_net_data,_socket_id);
			}
		}
	} else {
		show_message(string("Attempted to execute a non-existant network event : {0}" , _header));	
	}
}

define_network_events();

///Functions for update event

function client_callback() {
	if(!ds_queue_empty(network_client_data_callback_queue)) {
		var event_buffers = ds_map_create();
		
		while(!ds_queue_empty(network_client_data_callback_queue)) {
			var parameters = ds_queue_dequeue(network_client_data_callback_queue);	///paramaters are formatted [header,data]
			if(is_undefined(ds_map_find_value(event_buffers,parameters[0]))) {
				//If a buffer does not exist for the network event create one
				var buffer = buffer_create(2048,buffer_grow,1);
				buffer_seek(buffer,buffer_seek_start,0);
				buffer_write(buffer,buffer_string,parameters[0]);
				ds_map_add(event_buffers,parameters[0],buffer);
			}
			var buffer = ds_map_find_value(event_buffers,parameters[0]);
			buffer_write(buffer,buffer_bool,true); //let parser know to continue checking for data
			SnapBufferWriteBinary(buffer,parameters[1]) //write current data into buffer
		}
		
		var headers = ds_map_keys_to_array(event_buffers);
		var length = array_length(headers);
		for(var i = 0; i < length; i ++) {
			var buffer = ds_map_find_value(event_buffers,headers[i]);
			buffer_write(buffer,buffer_bool,false); //Terminate parsing the buffer
			network_send_packet(obj_net_client.socket,buffer,buffer_tell(buffer));
			buffer_delete(buffer);	
		}
	
		ds_map_destroy(event_buffers);
	}
}

function server_callback() {
	///Handle socket unique events (this is for data executed with single call signal network events where buffer data is not the same)
	var socket_array = ds_map_keys_to_array(network_server_data_callback_map);
	var size = array_length(socket_array);
	for(var i = 0; i < size; i ++){
		var socket = socket_array[i];
		var network_server_data_callback_queue = ds_map_find_value(network_server_data_callback_map,socket);
		
		if(!ds_queue_empty(network_server_data_callback_queue)) {
			var event_buffers = ds_map_create();
			
			while(!ds_queue_empty(network_server_data_callback_queue)) {
				var parameters = ds_queue_dequeue(network_server_data_callback_queue);	///paramaters are formatted [header,data]
				if(is_undefined(ds_map_find_value(event_buffers,parameters[0]))) {
					//If a buffer does not exist for the network event create one
					var buffer = buffer_create(2048,buffer_grow,1);
					buffer_seek(buffer,buffer_seek_start,0);
					buffer_write(buffer,buffer_string,parameters[0]);
					ds_map_add(event_buffers,parameters[0],buffer);
				}
				var buffer = ds_map_find_value(event_buffers,parameters[0]);
				buffer_write(buffer,buffer_bool,true); //let parser know to continue checking for data
				SnapBufferWriteBinary(buffer,parameters[1]);
			}
			var headers = ds_map_keys_to_array(event_buffers);
			var length = array_length(headers);
			for(var j = 0; j < length; j ++) {
				var buffer = ds_map_find_value(event_buffers,headers[j]);
				buffer_write(buffer,buffer_bool,false); //Terminate parsing the buffer
				network_send_packet(socket,buffer,buffer_tell(buffer));
				buffer_delete(buffer);	
			}
		
			ds_map_destroy(event_buffers);
		}
	}
	
	///Handle socket parallel events (this is for data with multi call signal network events where buffer data IS the same
	///by using the same buffer for multiple packets we can optimize significantly by only making one packet for each event)
	
	if(!ds_queue_empty(network_server_data_callback_queue_all)) {
		var event_buffers = ds_map_create();
		var event_ignore_sockets = ds_map_create();
		while(!ds_queue_empty(network_server_data_callback_queue_all)) {
			var parameters = ds_queue_dequeue(network_server_data_callback_queue_all);	///paramaters are formatted [header,data,[ignore sockets]]
			if(is_undefined(ds_map_find_value(event_buffers,parameters[0]))) {
				//If a buffer does not exist for the network event create one
				var buffer = buffer_create(2048,buffer_grow,1);
				buffer_seek(buffer,buffer_seek_start,0);
				buffer_write(buffer,buffer_string,parameters[0]);
				ds_map_add(event_buffers,parameters[0],buffer);
				ds_map_add(event_ignore_sockets,parameters[0],parameters[2]);
			}
			var buffer = ds_map_find_value(event_buffers,parameters[0]);
			buffer_write(buffer,buffer_bool,true); //let parser know to continue checking for data
			SnapBufferWriteBinary(buffer,parameters[1]) //write current data into buffer
		}
		
		var headers = ds_map_keys_to_array(event_buffers);
		var length = array_length(headers);
		
		var socket_list = obj_net_server.socket_list;
		var socket_list_length = ds_list_size(obj_net_server.socket_list);
		
		for(var i = 0; i < length; i ++) {
			var buffer = ds_map_find_value(event_buffers,headers[i]);
			var ignore_sockets = ds_map_find_value(event_ignore_sockets,headers[i]);
			buffer_write(buffer,buffer_bool,false); //Terminate parsing the buffer
			for(var j = 0; j < socket_list_length; j ++) {
				var socket = ds_list_find_value(socket_list,j);
				if(is_undefined(ignore_sockets)) {
					network_send_packet(socket,buffer,buffer_tell(buffer));
				} else if(!array_contains(ignore_sockets,socket)) {
					network_send_packet(socket,buffer,buffer_tell(buffer));
				}
			}
			buffer_delete(buffer);	
		}
	
		ds_map_destroy(event_buffers);
		ds_map_destroy(event_ignore_sockets);
	}
}


#event destroy
var array;
ds_map_values_to_array(network_events,array);
var length = array_length(array);
for(var i = 0; i < length; i ++) {
	ds_list_destroy(array[i]);	
}

ds_map_destroy(network_events);
ds_queue_destroy(network_client_data_callback_queue);

var map_callback_queues = ds_map_values_to_array(network_server_data_callback_map);
var length = array_length(map_callback_queues);
for(var i = 0; i < length; i ++) {
	ds_queue_destroy(map_callback_queues[i]);	
}
ds_map_destroy(network_server_data_callback_map);

#event step_begin
if(global.IS_CLIENT) {
	client_callback()
} else {
	server_callback()
}