$(document).ready(function () {
    $('.bigass-add-me-button').click(function (event) {
        var server_id = event.target.id.split('_')[4];
        var redirect_string = '/Interface/Login?guild_id=' + server_id;
        window.location.href = redirect_string;
    });
    $('.server-nav-item').click(function (event) {
        var server_id = $(event.target).parent().get(0).id.split('_')[2];
        var redirect_string = '/Dashboard/Servers?server_id=' + server_id;
        window.location.href = redirect_string;
    });
    $('.plugin-nav-item').click(function (event) {
        var plugin_nav_id = '#' + event.target.id;
        var plugin_tab_id = '#' + event.target.id.replace('PLUGIN_NAV', 'PLUGIN_CONTENT');
        $('.plugin-nav-item').toArray().forEach((element) => {
            var element_id = '#' + element.id;
            if (element_id == plugin_nav_id) {
                $(plugin_tab_id).show();
            }
            else {
                var invisible_plugin = '#' + element.id.replace('PLUGIN_NAV', 'PLUGIN_CONTENT');
                $(invisible_plugin).hide();
            }
        });
    });
    $('.role-add-button').click(function (event) {
        var target_id = '#' + event.target.id;
        var button = $(target_id);
        var data = button.attr('data-string');
        var data_container = button.attr('data-container');
        var popup_base = $('#role-select-popup-base');
        var popup_clone = $('#role-select-popup-clone');
        popup_clone.children().remove();
        var target_parent = button.parent().get(0);
        var role_exclude_list = new Array();
        //You'll need to check the parent container for all the existing roles to build the clone list for add/remove
        $(target_parent).children().get().forEach((current_role) => {
            var role_id_array = current_role.id.split('_');
            var role_id = role_id_array[3];
            role_exclude_list.push(role_id);
        });
        //Copy all the popup children into the clone
        popup_clone.append(popup_base.children().clone(true));
        //Update the ID of the cloned children to make them unique
        //Since the role-exclude list is populated, remove them from the clone
        popup_clone.children().get().forEach((popup_role) => {
            var new_id = popup_role.id.replace('base', 'clone');
            $(popup_role).attr('id', new_id);
            var child_role_id = popup_role.id.split('_')[1];
            if (role_exclude_list.includes(child_role_id)) {
                popup_role.remove();
            }
        });
        target_parent.append(popup_clone.get(0));
        popup_clone = $('#role-select-popup-clone');
        popup_clone.attr('data-string', data);
        popup_clone.attr('data-container', data_container);
        popup_clone.show();
    });
    $('.role-add-button-special').click(function (event) {
        var target_id = '#' + event.target.id;
        var button = $(target_id);
        var type = target_id.split('_')[1];
        var base_role = target_id.split('_')[6];
        $.ajax({
            type: 'GET',
            async: false,
            url: '/Dashboard/GetAllowedGroupingRoles',
            data: { base_role_id: base_role, group_type: type },
            cache: false,
            success: function (result) {
                var data_roles = result;
                var data_string = button.attr('data-string');
                var data_container = button.attr('data-container');
                var popup_base = $('#role-select-popup-base');
                var popup_clone = $('#role-select-popup-clone');
                popup_clone.children().remove();
                var target_parent = button.parent().get(0);
                var role_include_list = data_roles.split('_');
                var dash_user = data_string.split('_')[2];
                var base_role_id = data_string.split('_')[3];
                //If the role is valid in the Role Include List, add the clone to the select list
                popup_base.children().get().forEach((popup_role) => {
                    var new_id = popup_role.id + '_CLONE';
                    var role_clone = $(popup_role).clone(true);
                    var new_data_string = data_string.split('_')[0] + '_' + data_string.split('_')[1] + '_';
                    role_clone.attr('id', new_id);
                    //Set the class to dashboard-role-special for use in handling the event correctly
                    role_clone.attr('class', 'dashboard-special-role');
                    var server_id = role_clone.get(0).id.split('_')[0];
                    var child_role_id = role_clone.get(0).id.split('_')[1];
                    if (role_include_list.includes(child_role_id)) {
                        new_data_string = new_data_string + dash_user + '_' + server_id + '_' + base_role_id + '_' + child_role_id;
                        role_clone.attr('data-string', new_data_string);
                        popup_clone.append(role_clone.get(0));
                    }
                });
                target_parent.append(popup_clone.get(0));
                popup_clone = $('#role-select-popup-clone');
                popup_clone.attr('data-container', data_container);
                popup_clone.show();
            },
            failure: function (response) {
                return 'FAILURE';
            },
            error: function (response) {
                return 'ERROR';
            }
        });
    });
    $('.dashboard-role').click(function (event) {
        if ($(event.target).attr('class') === 'dashboard-special-role') { SpecialRoleClick(event); }
        else if ($(event.target).attr('class') === 'rolegroup-new-role') { RoleGroupRoleClick(event); }
        else {
            var popup = $('#role-select-popup-clone');
            var data = popup.attr('data-string');
            var container_array = popup.attr('data-container').split('_');
            var plugin = container_array[0].toLowerCase();
            var type = container_array[1].toLowerCase();
            var data_string = plugin + '_' + type + '_';
            var container = '#' + popup.attr('data-container');
            data = data + event.target.id;
            var user_id = data.split('_')[3];
            ApplyRoleSelect(data);
            //This is also where you add the role to the box
            var data_array = event.target.id.split('_');
            var server_id = data_array[0];
            var role_id = data_array[1];
            var base_id = '#' + server_id + "_" + role_id + "_ROLE_BASE";
            data_string = data_string + user_id + '_' + server_id + '_' + role_id;
            var new_role = $(base_id).clone(true);
            var parent = $(container);
            var new_role_id = container.replace('CONTAINER', role_id);
            new_role.attr('id', new_role_id);
            parent.append(new_role);
            button_elm = new_role.children('.role-remove-button').get(0);
            var new_button_id = button_elm.id.replace('PLUGIN', plugin.toUpperCase()).replace('TYPE', type.toUpperCase()).replace('_CLONE', '');
            var button = $(button_elm);
            button.attr('id', new_button_id);
            button.attr('data-string', data_string);
            //Remove all the children from the clone as it will be populated on next button click
            popup.children().remove();
            popup.hide();
        }
    });
    function RoleGroupRoleClick(event) {
        var popup = $('#role-select-popup-clone');
        var button = $('#ROLEGROUP_CREATE_BUTTON');
        var role_selected = $(event.target);
        var data_array = role_selected.get(0).id.split('_');
        var server_id = data_array[0];
        var role_id = data_array[1];
        var dash_user = popup.attr('data-string').split('_')[2];
        var excluded_roles = button.attr('data-roles');
        excluded_roles += '_' + role_id;
        button.attr('data_roles', excluded_roles);
        var role_style = $(event.target).attr('style').split(';')[0];
        var role_name = event.target.innerHTML;
        var role_group_template = $('#ROLEGROUP_SERVER_BASEROLE');
        var template_clone = role_group_template.clone(true);
        var template_id = template_clone.get(0).id.replace('SERVER', server_id).replace('BASEROLE', role_id);
        template_clone.attr('id', template_id);
        var base_clone = template_clone.children('#ROLEGROUP_BASE_SERVER_BASEROLE').get(0);
        var base_id = base_clone.id.replace('SERVER', server_id).replace('BASEROLE', role_id);
        $(base_clone).attr('id', base_id);
        $(base_clone).attr('style', role_style);
        base_clone.innerHTML = '<span class="role-name">' + role_name + '</span>';
        var grouped_container_clone = template_clone.children('#ROLEGROUP_GROUPED_ROLE_ADD_BUTTON_CONTAINER_SERVER_BASEROLE').get(0);
        var grouped_container_id = grouped_container_clone.id.replace('SERVER', server_id).replace('BASEROLE', role_id);
        $(grouped_container_clone).attr('id', grouped_container_id);
        var grouped_button_clone = $(grouped_container_clone).children('#ROLEGROUP_GROUPED_ROLE_ADD_BUTTON_SERVER_BASEROLE').get(0);
        var grouped_button_id = grouped_button_clone.id.replace('SERVER', server_id).replace('BASEROLE', role_id);
        $(grouped_button_clone).attr('id', grouped_button_id);
        var grouped_data_container = $(grouped_button_clone).attr('data-container').replace('SERVER', server_id).replace('BASEROLE', role_id);
        $(grouped_button_clone).attr('data-container', grouped_data_container);
        var rejected_container_clone = template_clone.children('#ROLEGROUP_REJECTED_ROLE_ADD_BUTTON_CONTAINER_SERVER_BASEROLE').get(0);
        var rejected_container_id = rejected_container_clone.id.replace('SERVER', server_id).replace('BASEROLE', role_id);
        $(rejected_container_clone).attr('id', rejected_container_id);
        var rejected_button_clone = $(rejected_container_clone).children('#ROLEGROUP_REJECTED_ROLE_ADD_BUTTON_SERVER_BASEROLE').get(0);
        var rejected_button_id = rejected_button_clone.id.replace('SERVER', server_id).replace('BASEROLE', role_id);
        $(rejected_button_clone).attr('id', rejected_button_id);
        var rejected_data_container = $(rejected_button_clone).attr('data-container').replace('SERVER', server_id).replace('BASEROLE', role_id);
        $(rejected_button_clone).attr('data-container', rejected_data_container);
        var required_container_clone = template_clone.children('#ROLEGROUP_REQUIRED_ROLE_ADD_BUTTON_CONTAINER_SERVER_BASEROLE').get(0);
        var required_container_id = required_container_clone.id.replace('SERVER', server_id).replace('BASEROLE', role_id);
        $(required_container_clone).attr('id', required_container_id);
        var required_button_clone = $(required_container_clone).children('#ROLEGROUP_REQUIRED_ROLE_ADD_BUTTON_SERVER_BASEROLE').get(0);
        var required_button_id = required_button_clone.id.replace('SERVER', server_id).replace('BASEROLE', role_id);
        $(required_button_clone).attr('id', required_button_id);
        var required_data_container = $(required_button_clone).attr('data-container').replace('SERVER', server_id).replace('BASEROLE', role_id);
        $(required_button_clone).attr('data-container', required_data_container);
        var role_group_container = $('#ROLEGROUP_COLLECTION_CONTAINER').get(0);
        role_group_container.append(template_clone.get(0));
        popup.children().remove();
        popup.hide();
        CommitRoleGroupCreate(server_id, role_id, dash_user);
        template_clone.show();
    }
    function CommitRoleGroupCreate(server, role, user) {
        $.ajax({
            type: 'GET',
            url: '/Dashboard/CommitRoleGroupCreated',
            data: { server_id: server, role_id: role, user_id: user },
            cache: false,
            success: function (result) {
                return result;
            },
            failure: function (response) {
                return false;
            },
            error: function (response) {
                return false;
            }
        });
    }
    function SpecialRoleClick(event) {
        var popup = $('#role-select-popup-clone');
        var data = $(event.target).attr('data-string');
        var container_array = popup.attr('data-container').split('_');
        var plugin = container_array[0].toLowerCase();
        var type = container_array[1].toLowerCase();
        var container = '#' + popup.attr('data-container');
        ApplyRoleSelect(data);
        //This is also where you add the role to the box
        var data_array = data.split('_');
        var dash_user = data_array[2];
        var server_id = data_array[3];
        var base_role_id = data_array[4];
        var role_id = data_array[5];
        var base_id = '#' + server_id + "_" + role_id + "_ROLE_BASE";
        var new_role = $(base_id).clone(true);
        var parent = $(container);
        var new_role_id = plugin.toUpperCase() + '_' + type.toUpperCase() + '_' + 'ROLE_' + base_role_id + '_' + role_id;
        new_role.attr('id', new_role_id);
        parent.append(new_role);
        button_elm = new_role.children('.role-remove-button').get(0);
        var new_button_id = button_elm.id.replace('PLUGIN', plugin.toUpperCase()).replace('TYPE', type.toUpperCase()).replace('_CLONE', '');
        var button = $(button_elm);
        button.attr('id', new_button_id);
        var data_string = plugin + '_' + type + '_' + dash_user + '_' + server_id + '_' + base_role_id + '_' + role_id;
        button.attr('data-string', data_string);
        //Remove all the children from the clone as it will be populated on next button click
        popup.children().remove();
        popup.hide();
        //Since this is going to affect the ability to add and remove to other roles, will need to update the data-roles attribute of each role add button
        //Since the database will have been updated, can run the algorithm with ajax to return the new data-string for each role add button
    }
    $('.role-remove-button').click(function (event) {
        var target_id = '#' + event.target.id;
        var button = $(target_id);
        var target_parent = button.parent().get(0);
        var data = button.attr('data-string');
        ApplyRoleRemoved(data);
        target_parent.remove();
    });
    $(window).click(function (event) { //ROLEGROUP_CREATE_BUTTON
        if (!(event.target.id === 'role-select-popup-clone' || event.target.id === 'ROLEGROUP_CREATE_BUTTON' || $(event.target).attr('class') === 'rolegroup-new-role' || $(event.target).attr('class') === 'dashboard-role' || $(event.target).attr('class') === 'role-add-button' || $(event.target).attr('class') === 'role-add-button-special' )) {
            $("#role-select-popup-clone").children().remove();
            $("#role-select-popup-clone").hide();
        }
    });
    function ApplyRoleSelect(data_value) {
        $.ajax({
            type: 'GET',
            url: '/Dashboard/CommitRoleAdded',
            data: { data_string: data_value },
            cache: false,
            success: function (result) {
                return result;
            },
            failure: function (response) {
                return false;
            },
            error: function (response) {
                return false;
            }
        });
    }
    function ApplyRoleRemoved(data_value) {
        $.ajax({
            type: 'GET',
            url: '/Dashboard/CommitRoleRemoved',
            data: { data_string: data_value },
            cache: false,
            success: function (result) {
                return result;
            },
            failure: function (response) {
                return false;
            },
            error: function (response) {
                return false;
            }
        });
    }
    $('#SETTINGS_HELPDESK_CREATE_BUTTON').click(function (event) {
        var button = $(event.target);
        var dash_user = button.attr('data-user');
        $.ajax({
            type: 'GET',
            url: '/Dashboard/CreateHelpdesk',
            data: { user_id: dash_user },
            cache: false,
            success: function (result) {
                return true;
            },
            failure: function (response) {
                return false;
            },
            error: function (response) {
                return false;
            }
        });
        $(event.target).attr('disabled', 'true');
        $('#SETTINGS_HELPDESK_DELETE_BUTTON').attr('disabled', 'false');
    });
    $('#SETTINGS_HELPDESK_DELETE_BUTTON').click(function (event) {
        var button = $(event.target);
        var dash_user = button.attr('data-user');
        $.ajax({
            type: 'GET',
            url: '/Dashboard/DeleteHelpdesk',
            data: { user_id: dash_user },
            cache: false,
            success: function (result) {
                return true;
            },
            failure: function (response) {
                return false;
            },
            error: function (response) {
                return false;
            }
        });
        $(event.target).attr('disabled', 'true');
        $('#SETTINGS_HELPDESK_CREATE_BUTTON').attr('disabled', 'false');
    });
    $('#ROLEGROUP_CREATE_BUTTON').click(function (event) {
        //This ilst of roles contains all the roles that are *not* available
        var target_id = '#' + event.target.id;
        var button = $(target_id);
        var role_exclude_string = $(event.target).attr('data-roles');
        var role_exclude_list = role_exclude_string.split('_');
        var popup_base = $('#role-select-popup-base');
        var popup_clone = $('#role-select-popup-clone');
        popup_clone.children().remove();
        var target_parent = button.parent().get(0);
        //Copy all the popup children into the clone
        popup_clone.append(popup_base.children().clone(true));
        //Update the ID of the cloned children to make them unique
        //Since the role-exclude list is populated, remove them from the clone
        popup_clone.children().get().forEach((popup_role) => {
            var new_id = popup_role.id.replace('base', 'clone');
            $(popup_role).attr('id', new_id);
            $(popup_role).attr('class', 'rolegroup-new-role');
            var child_role_id = popup_role.id.split('_')[1];
            if (role_exclude_list.includes(child_role_id)) {
                popup_role.remove();
            }
        });
        target_parent.append(popup_clone.get(0));
        popup_clone = $('#role-select-popup-clone');
        popup_clone.show();
    });
    $('.rolegroup-new-role').click(function (event) {
        //This is the class that will contain the base role from the list
        alert(event.target.id);
    });
    $('#BASIC_LINKUP_CHANNEL_CREATE_BUTTON').click(function (event) {
        var button = $(event.target);
        var dash_user = button.attr('data-user');
        $.ajax({
            type: 'GET',
            url: '/Dashboard/CreateBasicLinkup',
            data: { user_id: dash_user },
            cache: false,
            success: function (result) {
                return true;
            },
            failure: function (response) {
                return false;
            },
            error: function (response) {
                return false;
            }
        });
        $(event.target).attr('disabled', 'true');
        $('#BASIC_LINKUP_CHANNEL_DELETE_BUTTON').attr('disabled', 'false');
    });
    $('#BASIC_LINKUP_CHANNEL_DELETE_BUTTON').click(function (event) {
        var button = $(event.target);
        var dash_user = button.attr('data-user');
        $.ajax({
            type: 'GET',
            url: '/Dashboard/DeleteBasicLinkup',
            data: { user_id: dash_user },
            cache: false,
            success: function (result) {
                return true;
            },
            failure: function (response) {
                return false;
            },
            error: function (response) {
                return false;
            }
        });
        $(event.target).attr('disabled', 'true');
        $('#BASIC_LINKUP_CHANNEL_CREATE_BUTTON').attr('disabled', 'false');
    });
    $('#PREMIUM_LINKUP_CHANNEL_CREATE_BUTTON').click(function (event) {
        var button = $(event.target);
        var dash_user = button.attr('data-user');
        $.ajax({
            type: 'GET',
            url: '/Dashboard/CreatePremiumLinkup',
            data: { user_id: dash_user },
            cache: false,
            success: function (result) {
                return true;
            },
            failure: function (response) {
                return false;
            },
            error: function (response) {
                return false;
            }
        });
        $(event.target).attr('disabled', 'true');
        $('#PREMIUM_LINKUP_CHANNEL_DELETE_BUTTON').attr('disabled', 'false');
    });
    $('#PREMIUM_LINKUP_CHANNEL_DELETE_BUTTON').click(function (event) {
        var button = $(event.target);
        var dash_user = button.attr('data-user');
        $.ajax({
            type: 'GET',
            url: '/Dashboard/DeletePremiumLinkup',
            data: { user_id: dash_user },
            cache: false,
            success: function (result) {
                return true;
            },
            failure: function (response) {
                return false;
            },
            error: function (response) {
                return false;
            }
        });
        $(event.target).attr('disabled', 'true');
        $('#PREMIUM_LINKUP_CHANNEL_CREATE_BUTTON').attr('disabled', 'false');
    });
    $('#LOCKOUT_CHANNEL_CREATE_BUTTON').click(function (event) {
        $.ajax({
            type: 'GET',
            url: '/Dashboard/CreateLockoutChannel',
            data: {},
            cache: false,
            success: function (result) {
                return true;
            },
            failure: function (response) {
                return false;
            },
            error: function (response) {
                return false;
            }
        });
        $(event.target).attr('disabled', 'true');
    });
    $('#VERIFY_CHANNEL_CREATE_BUTTON').click(function (event) {
        $.ajax({
            type: 'GET',
            url: '/Dashboard/CreateVerifyChannel',
            data: {},
            cache: false,
            success: function (result) {
                return true;
            },
            failure: function (response) {
                return false;
            },
            error: function (response) {
                return false;
            }
        });
        $(event.target).attr('disabled', 'true');
    });
});