var AppDispatcher = require('../dispatcher/AppDispatcher');
var EventEmitter = require('events').EventEmitter;
var Configuration = require('../constants/Configuration');
var AppConstants = require('../constants/AppConstants');
var merge = require('react/lib/merge');

var USER_LOGIN_EVENT = 'user-login';

var _user = {loggedIn: false};

function getUserData(authResult) {
    gapi.auth.setToken(authResult);
    gapi.client.load('plus','v1', function(){
	var request = gapi.client.plus.people.get({'userId': 'me'});
	request.execute(function(resp) {
	    _user = {username: resp.name.givenName, 
		     loggedIn: true, 
		     userImage: resp.image.url};
            $.post('login', {googleId: resp.id, code: authResult.code}, function(loginOk) {
                if (loginOk) {
	            UserStore.emitChange();
                }
            });
	});
    });
};

var UserStore = merge(EventEmitter.prototype, {
  
    getUser: function() {
	return _user;
    },
    
    emitChange: function() {
	this.emit(USER_LOGIN_EVENT);
    },
    
    /**
     * @param {function} callback
     */
    addChangeListener: function(callback) {
	this.on(USER_LOGIN_EVENT, callback);
    },
    
    /**
     * @param {function} callback
     */
    removeChangeListener: function(callback) {
	this.removeListener(USER_LOGIN_EVENT, callback);
    }  
});

AppDispatcher.register(function(payload) {
    var action = payload.action;
    
    switch(action.actionType) {
	case AppConstants.LOGIN_USER:
	getUserData(payload.action.authResult);
	break;
    };


    return true; // No errors.  Needed by promise in Dispatcher.
});


module.exports = UserStore;
